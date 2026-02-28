using JSEA_Application.DTOs.Request.Journey;
using JSEA_Application.DTOs.Respone.Journey;
using JSEA_Application.Enums;
using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using JourneyEntity = JSEA_Application.Models.Journey;

namespace JSEA_Application.Services.Journey;

public class JourneyService : IJourneyService
{
    private readonly IJourneyRepository _journeyRepository;
    private readonly IGoongMapsService _goongMapsService;

    public JourneyService(IJourneyRepository journeyRepository, IGoongMapsService goongMapsService)
    {
        _journeyRepository = journeyRepository;
        _goongMapsService = goongMapsService;
    }

    public async Task<JourneySetupResponse?> ValidateAndCreateJourneyAsync(JourneySetupRequest request, Guid? travelerId, CancellationToken cancellationToken = default)
    {
        var routeContext = await _goongMapsService.AnalyzeRouteContextAsync(
            request.OriginAddress,
            request.DestinationAddress,
            request.VehicleType,
            request.TimeBudgetMinutes,
            request.MaxDetourDistanceMeters,
            cancellationToken);

        if (routeContext == null)
            return null;

        var journey = new JourneyEntity
        {
            TravelerId = travelerId,
            OriginAddress = request.OriginAddress,
            DestinationAddress = request.DestinationAddress,
            OriginLocation = routeContext.OriginLocation,
            DestinationLocation = routeContext.DestinationLocation,
            RoutePath = routeContext.RoutePath,
            TotalDistanceMeters = routeContext.TotalDistanceMeters,
            EstimatedDurationMinutes = routeContext.EstimatedDurationMinutes,
            VehicleType = request.VehicleType,
            TimeBudgetMinutes = request.TimeBudgetMinutes,
            MaxDetourDistanceMeters = request.MaxDetourDistanceMeters,
            Status = JourneyStatus.Planning
        };

        var waypoints = new List<JourneyWaypoint>
        {
            new()
            {
                Address = request.OriginAddress,
                Location = routeContext.OriginLocation,
                StopOrder = 0
            },
            new()
            {
                Address = request.DestinationAddress,
                Location = routeContext.DestinationLocation,
                StopOrder = 1
            }
        };

        var saved = await _journeyRepository.SaveAsync(journey, waypoints, cancellationToken);

        var summary = $"Tuyến ~{routeContext.TotalDistanceMeters / 1000.0:F1} km, ước tính ~{routeContext.EstimatedDurationMinutes} phút.";

        return new JourneySetupResponse
        {
            JourneyId = saved.Id,
            Status = saved.Status ?? JourneyStatus.Planning,
            Summary = summary
        };
    }
}
