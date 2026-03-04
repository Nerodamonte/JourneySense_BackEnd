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
    private readonly IWeatherService _weatherService;

    public JourneyService(
        IJourneyRepository journeyRepository,
        IGoongMapsService goongMapsService,
        IMicroExperienceRepository microExperienceRepository,
        IWeatherService weatherService)
    {
        _journeyRepository = journeyRepository;
        _goongMapsService = goongMapsService;
        _microExperienceRepository = microExperienceRepository;
        _weatherService = weatherService;
    }

    public async Task<JourneySetupResponse?> ValidateAndCreateJourneyAsync(JourneySetupRequest request, Guid? travelerId, CancellationToken cancellationToken = default)
    {
        if (!travelerId.HasValue)
            return null;

        var routeContext = await _goongMapsService.AnalyzeRouteContextAsync(
            request.OriginAddress,
            request.DestinationAddress,
            request.VehicleType,
            request.TimeBudgetMinutes,
            request.MaxDetourDistanceMeters,
            cancellationToken);

        if (routeContext == null)
            return null;

        var journey = new Models.Journey
        {
            TravelerId = travelerId.Value,
            OriginAddress = request.OriginAddress,
            DestinationAddress = request.DestinationAddress,
            OriginLocation = routeContext.OriginLocation,
            DestinationLocation = routeContext.DestinationLocation,
            RoutePath = routeContext.RoutePath,
            TotalDistanceMeters = routeContext.TotalDistanceMeters,
            EstimatedDurationMinutes = routeContext.EstimatedDurationMinutes,
            VehicleType = request.VehicleType.ToString().ToLowerInvariant(),
            TimeBudgetMinutes = request.TimeBudgetMinutes,
            MaxDetourDistanceMeters = request.MaxDetourDistanceMeters,
            CurrentMoodFactorId = null,
            PreferredStopDurationMinutes = request.PreferredStopDurationMinutes,
            MaxStops = request.MaxStopCount > 0 ? request.MaxStopCount : null,
            Status = "planning"
        };

        var waypoints = new List<JourneyWaypoint>();

        var saved = await _journeyRepository.SaveAsync(journey, waypoints, cancellationToken);

        var summary = $"Tuyến ~{routeContext.TotalDistanceMeters / 1000.0:F1} km, ước tính ~{routeContext.EstimatedDurationMinutes} phút.";

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
            PreferredStopDurationMinutes = saved.PreferredStopDurationMinutes,
            MaxStopCount = request.MaxStopCount
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
            CurrentMood = null,
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
            CurrentMood = null,
            PreferredStopDurationMinutes = j.PreferredStopDurationMinutes,
            Status = Enum.TryParse<JourneyStatus>(j.Status, true, out var js) ? js : null,
            StartedAt = j.StartedAt,
            CompletedAt = j.CompletedAt,
            CreatedAt = j.CreatedAt
        };
    }

    public async Task<List<RouteMicroExperienceSuggestionResponse>> GetSuggestionsAlongRouteAsync(Guid journeyId, int? limit, WeatherType? weather, TimeOfDay? timeOfDay, CancellationToken cancellationToken = default)
    {
        var journey = await _journeyRepository.GetByIdAsync(journeyId, cancellationToken);
        if (journey == null)
            return new List<RouteMicroExperienceSuggestionResponse>();

        var maxCount = limit is > 0 and <= 50 ? limit.Value : 20;
        var weatherStr = weather.HasValue ? weather.Value.ToString() : null;

        if (weatherStr == null && journey.OriginLocation != null)
        {
            var current = await _weatherService.GetCurrentWeatherAsync(journey.OriginLocation.Y, journey.OriginLocation.X, cancellationToken);
            if (current != null)
                weatherStr = current.WeatherType.ToString();
        }

        var timeOfDayStr = timeOfDay.HasValue ? timeOfDay.Value.ToString() : null;
        return await _microExperienceRepository.FindSuggestionsAlongRouteAsync(journeyId, maxCount, weatherStr, timeOfDayStr, cancellationToken);
    }
}
