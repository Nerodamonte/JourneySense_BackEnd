using JSEA_Application.Constants;
using JSEA_Application.DTOs.Request.Journey;
using JSEA_Application.DTOs.Respone.Journey;
using JSEA_Application.Enums;
using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using NetTopologySuite.Geometries;
using UserPackageEntity = JSEA_Application.Models.UserPackage;

namespace JSEA_Application.Services.Journey;

public class JourneyService : IJourneyService
{
    private readonly IJourneyRepository _journeyRepository;
    private readonly IGoongMapsService _goongMapsService;
    private readonly IMicroExperienceRepository _microExperienceRepository;
    private readonly IExperienceEmbeddingRepository _embeddingRepository;
    private readonly IVisitRepository _visitRepository;
    private readonly IUserPackageRepository _userPackageRepository;
    private readonly IPackageRepository _packageRepository;

    public JourneyService(
        IJourneyRepository journeyRepository,
        IGoongMapsService goongMapsService,
        IMicroExperienceRepository microExperienceRepository,
        IExperienceEmbeddingRepository embeddingRepository,
        IVisitRepository visitRepository,
        IUserPackageRepository userPackageRepository,
        IPackageRepository packageRepository)
    {
        _journeyRepository = journeyRepository;
        _goongMapsService = goongMapsService;
        _microExperienceRepository = microExperienceRepository;
        _embeddingRepository = embeddingRepository;
        _visitRepository = visitRepository;
        _userPackageRepository = userPackageRepository;
        _packageRepository = packageRepository;
    }

    private static int EstimateDetourMinutes(int detourMeters, string vehicleType)
    {
        var speedKmh = vehicleType.ToLowerInvariant() switch
        {
            "walking" => 5.0,
            "bicycle" => 15.0,
            "motorbike" => 35.0,
            "car" => 40.0,
            _ => 30.0
        };
        return (int)Math.Ceiling(detourMeters / 1000.0 / speedKmh * 60);
    }

    public async Task<JourneySetupResponse?> ValidateAndCreateJourneyAsync(JourneySetupRequest request, Guid? travelerId, CancellationToken cancellationToken = default)
    {
        if (!travelerId.HasValue)
            return null;

        List<RouteContext> routes;
        if (request.OriginLatitude.HasValue && request.OriginLongitude.HasValue &&
            request.DestinationLatitude.HasValue && request.DestinationLongitude.HasValue)
        {
            routes = await _goongMapsService.AnalyzeRouteContextByCoordinatesAsync(
                request.OriginLatitude.Value,
                request.OriginLongitude.Value,
                request.DestinationLatitude.Value,
                request.DestinationLongitude.Value,
                request.VehicleType,
                request.TimeBudgetMinutes,
                request.MaxDetourDistanceMeters,
                cancellationToken);
        }
        else
        {
            routes = await _goongMapsService.AnalyzeRouteContextAsync(
                request.OriginAddress ?? string.Empty,
                request.DestinationAddress ?? string.Empty,
                request.VehicleType,
                request.TimeBudgetMinutes,
                request.MaxDetourDistanceMeters,
                cancellationToken);
        }

        if (routes == null || routes.Count == 0)
            return null;

        var primaryRoute = routes[0];
        var userPackage = await EnsureTravelerHasActivePackageAsync(travelerId.Value, cancellationToken);
        var plannedKm = primaryRoute.TotalDistanceMeters / 1000m;
        EnsurePlannedDistanceWithinPackage(plannedKm, userPackage);

        // ExperienceCount: dọc tuyến + hard constraints + pool dừng (TimeBudgetMinutes) + đã có embedding.
        // TimeBudgetMinutes = ngân sách phút dừng/khám phá, không trừ ETA chính tuyến.
        var explorePoolMinutes = request.TimeBudgetMinutes;
        foreach (var route in routes)
        {
            if (route.RoutePath == null)
            {
                route.ExperienceCount = 0;
                continue;
            }

            if (explorePoolMinutes <= 0)
            {
                route.ExperienceCount = 0;
                continue;
            }

            var candidates = await _microExperienceRepository.FindCandidatesAsync(
                vehicleType: request.VehicleType.ToString().ToLowerInvariant(),
                preferredCrowdLevel: request.PreferredCrowdLevel.ToString().ToLowerInvariant(),
                segmentPath: route.RoutePath,
                maxDetourDistanceMeters: request.MaxDetourDistanceMeters,
                excludeIds: new List<Guid>(),
                cancellationToken: cancellationToken);

            var filteredCandidateIds = candidates
                .Where(e =>
                {
                    var distanceDeg = e.Location.Distance(route.RoutePath);
                    var distanceM = (int)Math.Round(distanceDeg * 111_000);
                    var detour = EstimateDetourMinutes(distanceM, request.VehicleType.ToString().ToLowerInvariant());
                    return detour <= explorePoolMinutes;
                })
                .Select(e => e.Id)
                .ToList();

            route.ExperienceCount = await _embeddingRepository.CountExistingAsync(filteredCandidateIds, cancellationToken);
        }

        var currentMood = request.CurrentMood.HasValue
            ? request.CurrentMood.Value.ToString()
            : null;

        var journey = new Models.Journey
        {
            TravelerId = travelerId.Value,
            OriginAddress = request.OriginAddress,
            DestinationAddress = request.DestinationAddress,
            OriginLocation = primaryRoute.OriginLocation,
            DestinationLocation = primaryRoute.DestinationLocation,
            RoutePath = primaryRoute.RoutePath,
            TotalDistanceMeters = primaryRoute.TotalDistanceMeters,
            EstimatedDurationMinutes = primaryRoute.EstimatedDurationMinutes,
            VehicleType = request.VehicleType.ToString().ToLowerInvariant(),
            TimeBudgetMinutes = request.TimeBudgetMinutes,
            MaxDetourDistanceMeters = request.MaxDetourDistanceMeters,
            CurrentMood = currentMood,
            MaxStops = request.MaxStopCount > 0 ? request.MaxStopCount : null,
            PreferredCrowdLevel = request.PreferredCrowdLevel.ToString().ToLowerInvariant(),
            Status = "planning"
        };

        var waypoints = new List<JourneyWaypoint>();

        // Tạo RouteSegment cho từng route, gán segmentId vào RouteContext để FE dùng gọi suggest.
        var segments = new List<RouteSegment>();
        foreach (var route in routes)
        {
            if (route.RoutePath == null) continue;

            var segment = new RouteSegment
            {
                Id = Guid.NewGuid(),
                JourneyId = journey.Id,
                SegmentPath = route.RoutePath,
                SegmentOrder = routes.IndexOf(route) + 1,
                DistanceMeters = route.TotalDistanceMeters,
                EstimatedDurationMinutes = route.EstimatedDurationMinutes
            };

            segments.Add(segment);
            route.SegmentId = segment.Id; // gán vào response
        }

        var saved = await _journeyRepository.SaveAsync(journey, waypoints, segments, cancellationToken);

        var summary = $"Tuyến ~{primaryRoute.TotalDistanceMeters / 1000.0:F1} km, ước tính ~{primaryRoute.EstimatedDurationMinutes} phút.";

        return new JourneySetupResponse
        {
            JourneyId = saved.Id,
            Status = JourneyStatus.Planning,
            Summary = summary,
            OriginAddress = saved.OriginAddress,
            DestinationAddress = saved.DestinationAddress,
            VehicleType = request.VehicleType,
            TimeBudgetMinutes = saved.TimeBudgetMinutes,
            MaxDetourDistanceMeters = saved.MaxDetourDistanceMeters,
            CurrentMood = request.CurrentMood,
            MaxStopCount = request.MaxStopCount,
            Routes = routes
        };
    }

    public async Task<List<JourneyListItemResponse>> GetMyJourneysAsync(Guid? travelerId, CancellationToken cancellationToken = default)
    {
        if (!travelerId.HasValue)
            return new List<JourneyListItemResponse>();

        var list = await _journeyRepository.GetByTravelerIdAsync(travelerId.Value, cancellationToken);
        return list.Select(j => new JourneyListItemResponse
        {
            Id = j.Id,
            OriginAddress = j.OriginAddress,
            DestinationAddress = j.DestinationAddress,
            VehicleType = Enum.TryParse<VehicleType>(j.VehicleType, true, out var vt) ? vt : null,
            TimeBudgetMinutes = j.TimeBudgetMinutes,
            CurrentMood = !string.IsNullOrEmpty(j.CurrentMood) && Enum.TryParse<MoodType>(j.CurrentMood, true, out var mood)
                ? mood
                : null,
            Status = Enum.TryParse<JourneyStatus>(j.Status, true, out var js) ? js : null,
            CreatedAt = j.CreatedAt,
            RoutePoints = JourneyRoutePointsHelper.FromJourney(j)
        }).ToList();
    }

    public async Task<JourneyDetailResponse?> GetByIdAsync(Guid id, Guid? viewerTravelerId, CancellationToken cancellationToken = default)
    {
        var j = await _journeyRepository.GetByIdAsync(id, cancellationToken);
        if (j == null)
            return null;

        var viewerIsOwner = viewerTravelerId.HasValue && viewerTravelerId.Value == j.TravelerId;
        var hasJourneyFeedback = !string.IsNullOrWhiteSpace(j.JourneyFeedback);
        var showJourneyFeedbackText = hasJourneyFeedback &&
            (viewerIsOwner || string.Equals(
                j.JourneyFeedbackModerationStatus,
                FeedbackModerationStatuses.Approved,
                StringComparison.OrdinalIgnoreCase));

        Guid? selectedSegmentId = null;
        if (j.JourneyWaypoints != null && j.JourneyWaypoints.Count > 0)
        {
            selectedSegmentId = j.JourneyWaypoints
                .Select(w => w.Suggestion?.SegmentId)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Distinct()
                .SingleOrDefault();
        }

        var routePoints = JourneyRoutePointsHelper.FromJourney(j);

        var segments = j.RouteSegments?
            .OrderBy(s => s.SegmentOrder ?? int.MaxValue)
            .Select(s => new RouteSegmentResponse
            {
                SegmentId = s.Id,
                SegmentOrder = s.SegmentOrder,
                DistanceMeters = s.DistanceMeters,
                EstimatedDurationMinutes = s.EstimatedDurationMinutes,
                IsScenic = s.IsScenic,
                IsBusy = s.IsBusy,
                IsCulturalArea = s.IsCulturalArea
            })
            .ToList();

        var visits = await _visitRepository.GetByJourneyTravelerAsync(j.Id, j.TravelerId, cancellationToken);
        var visitByExperienceId = visits
            .GroupBy(v => v.ExperienceId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.VisitedAt).First());

        var waypoints = j.JourneyWaypoints?
            .OrderBy(w => w.StopOrder)
            .Select(w =>
            {
                visitByExperienceId.TryGetValue(w.ExperienceId, out var visit);
                return new JourneyWaypointResponse
                {
                    WaypointId = w.Id,
                    ExperienceId = w.ExperienceId,
                    SuggestionId = w.SuggestionId,
                    SegmentId = w.Suggestion?.SegmentId,
                    StopOrder = w.StopOrder,
                    Name = w.Experience?.Name,
                    CategoryName = w.Experience?.Category?.Name,
                    Address = w.Experience?.Address,
                    City = w.Experience?.City,
                    Latitude = w.Experience?.Location?.Y,
                    Longitude = w.Experience?.Location?.X,
                    CoverPhotoUrl = w.Experience?.ExperiencePhotos?.FirstOrDefault(p => p.IsCover == true)?.PhotoUrl,
                    DetourDistanceMeters = w.Suggestion?.DetourDistanceMeters,
                    DetourTimeMinutes = w.Suggestion?.DetourTimeMinutes,
                    VisitFeedback = MapWaypointVisitFeedback(visit, j.TravelerId, viewerTravelerId)
                };
            })
            .ToList();

        return new JourneyDetailResponse
        {
            Id = j.Id,
            TravelerId = j.TravelerId,
            OriginAddress = j.OriginAddress,
            DestinationAddress = j.DestinationAddress,
            VehicleType = Enum.TryParse<VehicleType>(j.VehicleType, true, out var vt) ? vt : null,
            TotalDistanceMeters = j.TotalDistanceMeters,
            EstimatedDurationMinutes = j.EstimatedDurationMinutes,
            TimeBudgetMinutes = j.TimeBudgetMinutes,
            MaxDetourDistanceMeters = j.MaxDetourDistanceMeters,
            CurrentMood = !string.IsNullOrEmpty(j.CurrentMood) && Enum.TryParse<MoodType>(j.CurrentMood, true, out var mood)
                ? mood
                : null,
            Status = Enum.TryParse<JourneyStatus>(j.Status, true, out var js) ? js : null,
            StartedAt = j.StartedAt,
            CompletedAt = j.CompletedAt,
            CreatedAt = j.CreatedAt,
            JourneyFeedback = showJourneyFeedbackText ? j.JourneyFeedback : null,
            JourneyFeedbackModerationStatus = hasJourneyFeedback ? j.JourneyFeedbackModerationStatus : null,
            RoutePoints = routePoints,
            SetupPrimaryRoutePoints = JourneyRoutePointsHelper.SetupPrimaryRouteFromSegments(j),
            Segments = segments,
            Waypoints = waypoints,
            SelectedSegmentId = selectedSegmentId
        };
    }

    public async Task<bool> SaveSelectedWaypointsAsync(
        Guid journeyId,
        Guid travelerId,
        Guid segmentId,
        List<SaveWaypointItemRequest> waypoints,
        CancellationToken cancellationToken = default)
    {
        var journey = await _journeyRepository.GetByIdAsync(journeyId, cancellationToken);
        if (journey == null) return false;
        if (journey.TravelerId != travelerId) return false;

        var segment = journey.RouteSegments.FirstOrDefault(s => s.Id == segmentId);
        if (segment?.SegmentPath == null) return false;

        waypoints ??= new List<SaveWaypointItemRequest>();

        if (journey.MaxStops.HasValue && waypoints.Count > journey.MaxStops.Value)
            return false;

        var suggestionIds = waypoints.Select(w => w.SuggestionId).Distinct().ToList();
        if (suggestionIds.Count != waypoints.Count)
            return false;

        var suggestions = await _journeyRepository.GetSuggestionsByIdsAsync(suggestionIds, cancellationToken);
        if (suggestions.Count != suggestionIds.Count)
            return false;

        // Validate suggestions belong to the same journey & selected segment.
        if (suggestions.Any(s => s.JourneyId != journeyId || s.SegmentId != segmentId))
            return false;

        var userPackageForSegment = await EnsureTravelerHasActivePackageAsync(travelerId, cancellationToken);
        var segmentPlannedKm = (segment.DistanceMeters ?? 0) / 1000m;
        EnsurePlannedDistanceWithinPackage(segmentPlannedKm, userPackageForSegment);

        // Persist selected route into journey fields (so later steps use the chosen route).
        journey.RoutePath = segment.SegmentPath;
        journey.TotalDistanceMeters = segment.DistanceMeters;
        journey.EstimatedDurationMinutes = segment.EstimatedDurationMinutes;
        journey.UpdatedAt = DateTime.UtcNow;

        var suggestionsById = suggestions.ToDictionary(s => s.Id, s => s);

        // IMPORTANT UX: StopOrder should reflect forward order along the selected main route,
        // not the order user tapped items. We compute StopOrder by projecting each waypoint onto the
        // selected segment polyline and sorting by progress along that line.
        var routeLine = segment.SegmentPath;
        var waypointsWithProgress = waypoints
            .Select(w =>
            {
                var s = suggestionsById[w.SuggestionId];
                var p = s.Experience?.Location;
                var progressMeters = p == null ? double.MaxValue : GetAlongRouteMeters(routeLine, p);
                return new { Suggestion = s, ProgressMeters = progressMeters };
            })
            .OrderBy(x => x.ProgressMeters)
            .ToList();

        var newWaypoints = waypointsWithProgress
            .Select((x, idx) => new JourneyWaypoint
            {
                Id = Guid.NewGuid(),
                JourneyId = journeyId,
                ExperienceId = x.Suggestion.ExperienceId,
                SuggestionId = x.Suggestion.Id,
                StopOrder = idx + 1
            })
            .ToList();

        var existingAcceptedIds = await _journeyRepository.GetInteractionSuggestionIdsAsync(
            suggestionIds,
            InteractionType.Accepted,
            cancellationToken);

        var existingAcceptedSet = existingAcceptedIds.Count > 0
            ? existingAcceptedIds.ToHashSet()
            : new HashSet<Guid>();

        var interactions = suggestionIds
            .Where(id => !existingAcceptedSet.Contains(id))
            .Select(id => new SuggestionInteraction
            {
                Id = Guid.NewGuid(),
                SuggestionId = id,
                InteractionType = InteractionType.Accepted,
                InteractedAt = DateTime.UtcNow
            })
            .ToList();

        await _journeyRepository.ReplaceWaypointsAsync(
            journeyId,
            newWaypoints,
            interactions,
            cancellationToken);

        return true;
    }

    public async Task<bool> LogSuggestionInteractionAsync(
        Guid suggestionId,
        Guid travelerId,
        InteractionType interactionType,
        CancellationToken cancellationToken = default)
    {
        var suggestion = await _journeyRepository.GetSuggestionByIdAsync(suggestionId, cancellationToken);
        if (suggestion?.Journey == null) return false;
        if (suggestion.Journey.TravelerId != travelerId) return false;

        await _journeyRepository.AddSuggestionInteractionAsync(suggestionId, interactionType, cancellationToken);
        return true;
    }

    public async Task<bool> UpdateCurrentMoodAsync(
        Guid journeyId,
        Guid travelerId,
        MoodType? currentMood,
        CancellationToken cancellationToken = default)
    {
        var journey = await _journeyRepository.GetByIdAsync(journeyId, cancellationToken);
        if (journey == null) return false;
        if (journey.TravelerId != travelerId) return false;

        // Only allow mood changes while planning (before selecting waypoints).
        if (!string.Equals(journey.Status, "planning", StringComparison.OrdinalIgnoreCase))
            return false;

        var waypointCount = await _journeyRepository.GetAcceptedWaypointCountAsync(journeyId, cancellationToken);
        if (waypointCount > 0)
            return false;

        journey.CurrentMood = currentMood?.ToString();
        journey.UpdatedAt = DateTime.UtcNow;

        await _journeyRepository.UpdateAsync(journey, cancellationToken);

        // Clear existing suggestions so suggest() regenerates with new mood.
        await _journeyRepository.ClearSuggestionsForJourneyAsync(journeyId, cancellationToken);

        return true;
    }

    public async Task<JourneyPolylineResponse?> GetJourneyPolylineAsync(
        Guid journeyId,
        Guid travelerId,
        CancellationToken cancellationToken = default)
    {
        var journey = await _journeyRepository.GetByIdAsync(journeyId, cancellationToken);
        if (journey == null)
            throw new KeyNotFoundException("Không tìm thấy hành trình.");
        if (journey.TravelerId != travelerId)
            throw new UnauthorizedAccessException("Không có quyền truy cập hành trình.");
        if (journey.OriginLocation == null || journey.DestinationLocation == null)
            throw new InvalidOperationException("Hành trình thiếu tọa độ origin/destination.");

        var waypointPoints = journey.JourneyWaypoints?
            .OrderBy(w => w.StopOrder)
            .Select(w => w.Experience?.Location)
            .Where(p => p != null)
            .Select(p => p!)
            .ToList() ?? new List<Point>();

        // Nếu chưa có waypoint thì fallback trả về tuyến đã lưu trong DB (route segment đã chọn).
        if (waypointPoints.Count == 0)
        {
            var line = journey.RoutePath ?? journey.ActualRoutePath;
            var points = line?.Coordinates
                .Select(c => new GeoPointResponse { Latitude = c.Y, Longitude = c.X })
                .ToList() ?? new List<GeoPointResponse>();

            return new JourneyPolylineResponse
            {
                JourneyId = journey.Id,
                Polyline = null,
                Points = points,
                DistanceMeters = journey.TotalDistanceMeters,
                EstimatedDurationMinutes = journey.EstimatedDurationMinutes
            };
        }

        var vehicle = Enum.TryParse<VehicleType>(journey.VehicleType, true, out var vt)
            ? vt
            : VehicleType.Car;

        var route = await _goongMapsService.GetDirectionRouteAsync(
            journey.OriginLocation,
            journey.DestinationLocation,
            vehicle,
            waypointPoints,
            cancellationToken);

        if (route == null)
            return null;

        var routePoints = route.RoutePath?.Coordinates
            .Select(c => new GeoPointResponse { Latitude = c.Y, Longitude = c.X })
            .ToList() ?? new List<GeoPointResponse>();

        return new JourneyPolylineResponse
        {
            JourneyId = journey.Id,
            TargetWaypointId = null,
            TargetExperienceId = null,
            Polyline = route.Polyline,
            Points = routePoints,
            DistanceMeters = route.TotalDistanceMeters,
            EstimatedDurationMinutes = route.EstimatedDurationMinutes
        };
    }

    public async Task<JourneyPolylineResponse?> GetNearestWaypointPolylineAsync(
        Guid journeyId,
        Guid travelerId,
        double currentLatitude,
        double currentLongitude,
        bool excludeCompletedWaypoints = true,
        CancellationToken cancellationToken = default)
    {
        var journey = await _journeyRepository.GetByIdAsync(journeyId, cancellationToken);
        if (journey == null)
            throw new KeyNotFoundException("Không tìm thấy hành trình.");
        if (journey.TravelerId != travelerId)
            throw new UnauthorizedAccessException("Không có quyền truy cập hành trình.");

        var candidates = journey.JourneyWaypoints
            ?.Where(w => w.Experience?.Location != null)
            .Where(w => !excludeCompletedWaypoints || w.ActualDepartureAt == null)
            .Select(w => new WaypointCandidate(w, w.Experience!.Location!))
            .ToList() ?? new List<WaypointCandidate>();

        var vehicle = Enum.TryParse<VehicleType>(journey.VehicleType, true, out var vt)
            ? vt
            : VehicleType.Car;

        var origin = new Point(currentLongitude, currentLatitude) { SRID = 4326 };

        // If no remaining waypoints, keep the same endpoint behavior and route user to destination.
        if (candidates.Count == 0)
        {
            if (journey.DestinationLocation == null)
                throw new InvalidOperationException("Hành trình thiếu tọa độ destination.");

            var routeToDestination = await _goongMapsService.GetDirectionRouteAsync(
                origin,
                journey.DestinationLocation,
                vehicle,
                waypoints: null,
                cancellationToken);

            if (routeToDestination == null)
                return null;

            var destPoints = routeToDestination.RoutePath?.Coordinates
                .Select(c => new GeoPointResponse { Latitude = c.Y, Longitude = c.X })
                .ToList() ?? new List<GeoPointResponse>();

            return new JourneyPolylineResponse
            {
                JourneyId = journey.Id,
                TargetWaypointId = null,
                TargetExperienceId = null,
                Polyline = routeToDestination.Polyline,
                Points = destPoints,
                DistanceMeters = routeToDestination.TotalDistanceMeters,
                EstimatedDurationMinutes = routeToDestination.EstimatedDurationMinutes
            };
        }

        // Prefer the next waypoint that is "ahead" along the main route (forward progress),
        // to avoid confusing backtracking when a waypoint is geometrically close but behind.
        var routeLine = journey.RoutePath ?? journey.ActualRoutePath;
        WaypointCandidate selected;
        if (routeLine != null)
        {
            var currentProgress = GetAlongRouteMeters(routeLine, origin);

            var byProgress = candidates
                .Select(c => new
                {
                    Candidate = c,
                    ProgressMeters = GetAlongRouteMeters(routeLine, c.Location)
                })
                .ToList();

            const double epsilonMeters = 50; // tolerate small GPS/projection noise

            var ahead = byProgress
                .Where(x => x.ProgressMeters >= currentProgress - epsilonMeters)
                .OrderBy(x => x.ProgressMeters)
                .FirstOrDefault();

            if (ahead != null)
            {
                selected = ahead.Candidate;
            }
            else
            {
                // If user has passed all remaining waypoints (rare), pick the closest-behind by progress
                // to minimize backtracking.
                selected = byProgress
                    .OrderByDescending(x => x.ProgressMeters)
                    .First().Candidate;
            }
        }
        else
        {
            // Fallback: if we don't have a route line stored, use Haversine-nearest.
            selected = candidates
                .Select(c => new
                {
                    Candidate = c,
                    DistanceMeters = HaversineDistanceMeters(currentLatitude, currentLongitude, c.Location.Y, c.Location.X)
                })
                .OrderBy(x => x.DistanceMeters)
                .First().Candidate;
        }

        var destination = new Point(selected.Location.X, selected.Location.Y) { SRID = 4326 };

        var route = await _goongMapsService.GetDirectionRouteAsync(
            origin,
            destination,
            vehicle,
            waypoints: null,
            cancellationToken);

        if (route == null)
            return null;

        var routePoints = route.RoutePath?.Coordinates
            .Select(c => new GeoPointResponse { Latitude = c.Y, Longitude = c.X })
            .ToList() ?? new List<GeoPointResponse>();

        return new JourneyPolylineResponse
        {
            JourneyId = journey.Id,
            TargetWaypointId = selected.Waypoint.Id,
            TargetExperienceId = selected.Waypoint.ExperienceId,
            Polyline = route.Polyline,
            Points = routePoints,
            DistanceMeters = route.TotalDistanceMeters,
            EstimatedDurationMinutes = route.EstimatedDurationMinutes
        };
    }

    public async Task<bool> UpdateJourneyFeedbackAsync(
        Guid journeyId,
        Guid travelerId,
        string? journeyFeedback,
        CancellationToken cancellationToken = default)
    {
        var journey = await _journeyRepository.GetBasicByIdAsync(journeyId, cancellationToken);
        if (journey == null || journey.TravelerId != travelerId)
            return false;

        if (string.IsNullOrWhiteSpace(journeyFeedback))
        {
            journey.JourneyFeedback = null;
            journey.JourneyFeedbackModerationStatus = FeedbackModerationStatuses.Approved;
        }
        else
        {
            journey.JourneyFeedback = journeyFeedback.Trim();
            journey.JourneyFeedbackModerationStatus = FeedbackModerationStatuses.Pending;
        }

        journey.UpdatedAt = DateTime.UtcNow;
        await _journeyRepository.UpdateAsync(journey, cancellationToken);
        return true;
    }

    private static JourneyWaypointVisitFeedbackResponse? MapWaypointVisitFeedback(
        Visit? visit,
        Guid journeyOwnerTravelerId,
        Guid? viewerTravelerId)
    {
        if (visit == null)
            return null;

        var fb = visit.Feedback;
        var rating = visit.Rating;
        if (fb == null && rating == null)
            return null;

        var viewerIsOwner = viewerTravelerId.HasValue && viewerTravelerId.Value == journeyOwnerTravelerId;
        var showText = fb == null || viewerIsOwner ||
            string.Equals(fb.ModerationStatus, FeedbackModerationStatuses.Approved, StringComparison.OrdinalIgnoreCase);

        return new JourneyWaypointVisitFeedbackResponse
        {
            VisitId = visit.Id,
            FeedbackId = fb?.Id,
            FeedbackText = showText ? fb?.FeedbackText : null,
            ModerationStatus = fb?.ModerationStatus,
            FeedbackCreatedAt = fb?.CreatedAt,
            Rating = rating?.Rating1
        };
    }

    private sealed record WaypointCandidate(JourneyWaypoint Waypoint, Point Location);

    private static int HaversineDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // meters
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return (int)Math.Round(R * c);
    }

    private static double GetAlongRouteMeters(LineString routeLine, Point point)
    {
        var coords = routeLine.Coordinates;
        if (coords == null || coords.Length < 2)
            return 0;

        var px = point.X;
        var py = point.Y;

        double bestDistanceMeters = double.MaxValue;
        double bestAlongMeters = 0;

        double cumulativeMeters = 0;

        for (var i = 0; i < coords.Length - 1; i++)
        {
            var ax = coords[i].X;
            var ay = coords[i].Y;
            var bx = coords[i + 1].X;
            var by = coords[i + 1].Y;

            var abx = bx - ax;
            var aby = by - ay;
            var apx = px - ax;
            var apy = py - ay;

            var abLen2 = abx * abx + aby * aby;
            var t = abLen2 <= 0 ? 0 : (apx * abx + apy * aby) / abLen2;
            if (t < 0) t = 0;
            if (t > 1) t = 1;

            var projX = ax + t * abx;
            var projY = ay + t * aby;

            var distToSegmentMeters = HaversineDistanceMeters(py, px, projY, projX);
            if (distToSegmentMeters < bestDistanceMeters)
            {
                bestDistanceMeters = distToSegmentMeters;

                var segMeters = HaversineDistanceMeters(ay, ax, by, bx);
                bestAlongMeters = cumulativeMeters + t * segMeters;
            }

            cumulativeMeters += HaversineDistanceMeters(ay, ax, by, bx);
        }

        return bestAlongMeters;
    }

    /// <summary>Gói đang active hoặc tạo gói Basic mặc định (user đăng ký cũ chưa có bản ghi).</summary>
    private async Task<UserPackageEntity> EnsureTravelerHasActivePackageAsync(Guid travelerId, CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;
        var current = await _userPackageRepository.GetCurrentByUserIdAsync(travelerId, nowUtc, cancellationToken);
        if (current != null)
            return current;

        var basicType = PackageType.Basic.ToString().ToLowerInvariant();
        var packages = await _packageRepository.GetListAsync(true, cancellationToken);
        var basic = packages.FirstOrDefault(p => p.Type == basicType);
        if (basic == null)
            throw new InvalidOperationException("Hệ thống chưa cấu hình gói Basic đang hoạt động.");

        return await _userPackageRepository.CreateAsync(new UserPackageEntity
        {
            UserId = travelerId,
            PackageId = basic.Id,
            DistanceLimitKm = basic.DistanceLimitKm,
            UsedKm = 0,
            IsActive = true,
            ActivatedAt = nowUtc,
            ExpiresAt = basic.DurationInDays <= 0 ? null : nowUtc.AddDays(basic.DurationInDays)
        }, cancellationToken);
    }

    /// <summary>Tổng km gói (snapshot <c>distance_limit_km</c>) trừ <c>used_km</c> = phần còn được phép chọn cho tuyến mới.</summary>
    private static void EnsurePlannedDistanceWithinPackage(decimal plannedKm, UserPackageEntity userPackage)
    {
        var remainingKm = (decimal)userPackage.DistanceLimitKm - userPackage.UsedKm;
        if (plannedKm > remainingKm)
            throw new InvalidOperationException(
                $"Quãng đường tuyến chính (~{plannedKm:F1} km) vượt hạn mức còn lại ({remainingKm:F1} km trong gói {userPackage.DistanceLimitKm} km). Vui lòng chọn tuyến ngắn hơn hoặc nâng cấp gói.");
    }

    private static double DegreesToRadians(double degrees) => degrees * (Math.PI / 180.0);

}