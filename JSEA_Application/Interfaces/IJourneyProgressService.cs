using JSEA_Application.DTOs.Request.JourneyProgress;
using JSEA_Application.DTOs.Respone.JourneyProgress;

namespace JSEA_Application.Interfaces;

public interface IJourneyProgressService
{
    Task<StartJourneyResponse?> StartJourneyAsync(Guid journeyId, Guid travelerId, CancellationToken cancellationToken = default);

    Task<WaypointCheckInResponse?> CheckInAsync(
        Guid journeyId,
        Guid waypointId,
        Guid travelerId,
        WaypointCheckInRequest request,
        CancellationToken cancellationToken = default);

    Task<WaypointCheckOutResponse?> CheckOutAsync(
        Guid journeyId,
        Guid waypointId,
        Guid travelerId,
        WaypointCheckOutRequest request,
        CancellationToken cancellationToken = default);
}
