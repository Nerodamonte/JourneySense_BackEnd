using JSEA_Application.DTOs.Request.Journey;
using JSEA_Application.DTOs.Respone.Journey;
using JSEA_Application.Enums;
using JSEA_Application.Interfaces;
using JSEA_Application.Models;

namespace JSEA_Application.Services.Journey;

public class JourneyService : IJourneyService
{
    private readonly IJourneyRepository _journeyRepository;
    private readonly IGoongMapsService _goongMapsService;
    private readonly IMicroExperienceRepository _microExperienceRepository;


    public JourneyService(
        IJourneyRepository journeyRepository,
        IGoongMapsService goongMapsService,
        IMicroExperienceRepository microExperienceRepository)

    {
        _journeyRepository = journeyRepository;
        _goongMapsService = goongMapsService;
        _microExperienceRepository = microExperienceRepository;
    }

    public async Task<JourneySetupResponse?> ValidateAndCreateJourneyAsync(JourneySetupRequest request, Guid? travelerId, CancellationToken cancellationToken = default)
    {
        if (!travelerId.HasValue)
            return null;

        var routes = await _goongMapsService.AnalyzeRouteContextAsync(
            request.OriginAddress,
            request.DestinationAddress,
            request.VehicleType,
            request.TimeBudgetMinutes,
            request.MaxDetourDistanceMeters,
            cancellationToken);

        if (routes == null || routes.Count == 0)
            return null;

        // Tính số lượng experiences phù hợp dọc theo từng tuyến (dựa trên filter cứng + status).
        foreach (var route in routes)
        {
            route.ExperienceCount = await _microExperienceRepository.CountAlongRouteAsync(
                route.RoutePath,
                request.MaxDetourDistanceMeters,
                cancellationToken);
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

        // Tạo RouteSegment từ RoutePath của primaryRoute.
        // SuggestService cần segment.SegmentPath để hard-filter + tính distance.
        var segments = new List<RouteSegment>();
        if (primaryRoute.RoutePath != null)
        {
            segments.Add(new RouteSegment
            {
                Id = Guid.NewGuid(),
                JourneyId = journey.Id,
                SegmentPath = primaryRoute.RoutePath,
                SegmentOrder = 1,
                DistanceMeters = primaryRoute.TotalDistanceMeters,
                EstimatedDurationMinutes = primaryRoute.EstimatedDurationMinutes
            });
          
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
            CreatedAt = j.CreatedAt
        };
    }

}