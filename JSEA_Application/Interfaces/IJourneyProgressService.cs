using JSEA_Application.DTOs.Request.JourneyProgress;
using JSEA_Application.DTOs.Respone.JourneyProgress;

namespace JSEA_Application.Interfaces;

public interface IJourneyProgressService
{
    Task<StartJourneyResponse?> StartJourneyAsync(Guid journeyId, Guid travelerId, CancellationToken cancellationToken = default);

    Task<CompleteJourneyResponse?> CompleteJourneyAsync(
        Guid journeyId,
        Guid travelerId,
        CancellationToken cancellationToken = default);

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

    Task<WaypointSkipResponse?> SkipWaypointAsync(
        Guid journeyId,
        Guid waypointId,
        Guid travelerId,
        CancellationToken cancellationToken = default);

    Task<WaypointCheckInResponse?> CheckInGuestAsync(
        Guid journeyId,
        Guid waypointId,
        Guid guestKey,
        WaypointCheckInRequest request,
        CancellationToken cancellationToken = default);

    Task<WaypointCheckOutResponse?> CheckOutGuestAsync(
        Guid journeyId,
        Guid waypointId,
        Guid guestKey,
        WaypointCheckOutRequest request,
        CancellationToken cancellationToken = default);

    Task<WaypointSkipResponse?> SkipWaypointGuestAsync(
        Guid journeyId,
        Guid waypointId,
        Guid guestKey,
        CancellationToken cancellationToken = default);

    Task<DestinationCheckpointResponse?> DestinationCheckInAsync(
        Guid journeyId,
        Guid travelerId,
        CancellationToken cancellationToken = default);

    Task<DestinationCheckpointResponse?> DestinationCheckInGuestAsync(
        Guid journeyId,
        Guid guestKey,
        CancellationToken cancellationToken = default);

    Task<DestinationCheckpointResponse?> DestinationCheckOutAsync(
        Guid journeyId,
        Guid travelerId,
        CancellationToken cancellationToken = default);

    Task<DestinationCheckpointResponse?> DestinationCheckOutGuestAsync(
        Guid journeyId,
        Guid guestKey,
        CancellationToken cancellationToken = default);
}
