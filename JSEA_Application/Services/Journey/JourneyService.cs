using JSEA_Application.DTOs.Request.Journey;
using JSEA_Application.DTOs.Respone.Journey;
using JSEA_Application.Enums;
using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using NetTopologySuite.Geometries;

namespace JSEA_Application.Services.Journey;

public class JourneyService : IJourneyService
{
    private readonly IJourneyRepository _journeyRepository;
    private readonly IGoongMapsService _goongMapsService;
    private readonly IMicroExperienceRepository _microExperienceRepository;
    private readonly IExperienceEmbeddingRepository _embeddingRepository;


    public JourneyService(
        IJourneyRepository journeyRepository,
        IGoongMapsService goongMapsService,
        IMicroExperienceRepository microExperienceRepository,
        IExperienceEmbeddingRepository embeddingRepository)

    {
        _journeyRepository = journeyRepository;
        _goongMapsService = goongMapsService;
        _microExperienceRepository = microExperienceRepository;
        _embeddingRepository = embeddingRepository;
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

        // ExperienceCount ở setup: dọc tuyến + hard constraints + (time-budget) + chỉ tính những experience đã có embedding.
        // Không giới hạn topK ở đây; count phản ánh số điểm có thể tính cosine/finalSimilarity để FE pin/ranking về sau.
        foreach (var route in routes)
        {
            if (route.RoutePath == null)
            {
                route.ExperienceCount = 0;
                continue;
            }

            // totalTimeBudget = base route time (Goong) + detour + stop.
            var baseRouteMinutes = route.EstimatedDurationMinutes;
            var remainingExtraMinutes = request.TimeBudgetMinutes - baseRouteMinutes;

            if (remainingExtraMinutes <= 0)
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

            // Filter theo remainingExtraMinutes (detour time) tương tự suggest.
            var filteredCandidateIds = candidates
                .Where(e =>
                {
                    var distanceDeg = e.Location.Distance(route.RoutePath);
                    var distanceM = (int)Math.Round(distanceDeg * 111_000);
                    var detour = EstimateDetourMinutes(distanceM, request.VehicleType.ToString().ToLowerInvariant());
                    return detour <= remainingExtraMinutes;
                })
                .Select(e => e.Id)
                .ToList();

            route.ExperienceCount = await _embeddingRepository.CountExistingAsync(filteredCandidateIds, cancellationToken);
        }

        var primaryRoute = routes[0];

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
            CreatedAt = j.CreatedAt
        }).ToList();
    }

    public async Task<JourneyDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var j = await _journeyRepository.GetByIdAsync(id, cancellationToken);
        if (j == null)
            return null;

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

        var routePoints = j.RoutePath != null
            ? j.RoutePath.Coordinates
                .Select(c => new GeoPointResponse { Latitude = c.Y, Longitude = c.X })
                .ToList()
            : null;

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

        var waypoints = j.JourneyWaypoints?
            .OrderBy(w => w.StopOrder)
            .Select(w => new JourneyWaypointResponse
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
                DetourTimeMinutes = w.Suggestion?.DetourTimeMinutes
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
            JourneyFeedback = j.JourneyFeedback,
            RoutePoints = routePoints,
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

        var stopOrders = waypoints.Select(w => w.StopOrder).ToList();
        if (stopOrders.Count != stopOrders.Distinct().Count())
            return false;

        var suggestions = await _journeyRepository.GetSuggestionsByIdsAsync(suggestionIds, cancellationToken);
        if (suggestions.Count != suggestionIds.Count)
            return false;

        // Validate suggestions belong to the same journey & selected segment.
        if (suggestions.Any(s => s.JourneyId != journeyId || s.SegmentId != segmentId))
            return false;

        // Time budget check: base route minutes + Σ detour minutes.
        // Planned stop duration is no longer provided by client during setup.
        var baseMinutes = segment.EstimatedDurationMinutes ?? 0;
        var totalDetourMinutes = suggestions.Sum(s => s.DetourTimeMinutes ?? 0);
        var totalTripMinutes = baseMinutes + totalDetourMinutes;

        var budgetMinutes = journey.TimeBudgetMinutes ?? 0;
        if (budgetMinutes > 0 && totalTripMinutes > budgetMinutes)
            return false;

        // Persist selected route into journey fields (so later steps use the chosen route).
        journey.RoutePath = segment.SegmentPath;
        journey.TotalDistanceMeters = segment.DistanceMeters;
        journey.EstimatedDurationMinutes = segment.EstimatedDurationMinutes;
        journey.UpdatedAt = DateTime.UtcNow;

        var suggestionsById = suggestions.ToDictionary(s => s.Id, s => s);

        var newWaypoints = waypoints
            .OrderBy(w => w.StopOrder)
            .Select(w =>
            {
                var s = suggestionsById[w.SuggestionId];
                return new JourneyWaypoint
                {
                    Id = Guid.NewGuid(),
                    JourneyId = journeyId,
                    ExperienceId = s.ExperienceId,
                    SuggestionId = s.Id,
                    StopOrder = w.StopOrder
                };
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

        if (candidates.Count == 0)
            throw new InvalidOperationException("Không có waypoint hợp lệ để tạo polyline.");

        // Chọn waypoint gần nhất theo khoảng cách đường chim bay (Haversine) để tránh gọi N lần Directions.
        var nearest = candidates
            .Select(c => new
            {
                c.Waypoint,
                c.Location,
                DistanceMeters = HaversineDistanceMeters(currentLatitude, currentLongitude, c.Location.Y, c.Location.X)
            })
            .OrderBy(x => x.DistanceMeters)
            .First();

        var vehicle = Enum.TryParse<VehicleType>(journey.VehicleType, true, out var vt)
            ? vt
            : VehicleType.Car;

        var origin = new Point(currentLongitude, currentLatitude) { SRID = 4326 };
        var destination = new Point(nearest.Location.X, nearest.Location.Y) { SRID = 4326 };

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
            TargetWaypointId = nearest.Waypoint.Id,
            TargetExperienceId = nearest.Waypoint.ExperienceId,
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

        journey.JourneyFeedback = string.IsNullOrWhiteSpace(journeyFeedback) ? null : journeyFeedback.Trim();
        journey.UpdatedAt = DateTime.UtcNow;
        await _journeyRepository.UpdateAsync(journey, cancellationToken);
        return true;
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

    private static double DegreesToRadians(double degrees) => degrees * (Math.PI / 180.0);

}