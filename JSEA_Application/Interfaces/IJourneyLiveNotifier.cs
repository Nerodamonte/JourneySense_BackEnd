using JSEA_Application.DTOs.Journey;
using JSEA_Application.DTOs.Respone.Journey;

namespace JSEA_Application.Interfaces;

public interface IJourneyLiveNotifier
{
    /// <summary>
    /// Gửi tới group SignalR của journey (đã JoinJourney).
    /// </summary>
    Task NotifyDestinationMemberArrivedAsync(
        JourneyDestinationArrivedNotification notification,
        CancellationToken cancellationToken = default);

    /// <summary>Thành viên chọn loại khẩn cấp + điểm đến — broadcast cho group journey.</summary>
    Task NotifyEmergencyPlaceSelectedAsync(
        JourneyEmergencySelectionNotification notification,
        CancellationToken cancellationToken = default);

    /// <summary>Vị trí GPS realtime — từ hub <c>UpdateLocation</c>.</summary>
    Task NotifyMemberLocationAsync(
        JourneyMemberLocationNotification notification,
        CancellationToken cancellationToken = default);

    /// <summary>Snapshot tooltip x/N sau check-in / checkout / skip waypoint.</summary>
    Task NotifyWaypointAttendanceUpdatedAsync(
        JourneyWaypointAttendanceResponse snapshot,
        CancellationToken cancellationToken = default);

    Task NotifyMemberJoinedAsync(
        JourneyMemberJoinedNotification notification,
        CancellationToken cancellationToken = default);

    Task NotifyMemberLeftAsync(
        JourneyMemberLeftNotification notification,
        CancellationToken cancellationToken = default);

    Task NotifyJourneyStartedAsync(
        JourneyStartedLiveNotification notification,
        CancellationToken cancellationToken = default);
}
