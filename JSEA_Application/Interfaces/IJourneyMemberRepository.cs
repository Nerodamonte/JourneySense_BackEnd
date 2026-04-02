using JSEA_Application.DTOs.Respone.Journey;
using JSEA_Application.Models;

namespace JSEA_Application.Interfaces;

public interface IJourneyMemberRepository
{
    Task<JourneyMember?> GetActiveByTravelerAsync(Guid journeyId, Guid travelerId, CancellationToken cancellationToken = default);

    Task<JourneyMember?> GetActiveByGuestKeyAsync(Guid journeyId, Guid guestKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<JourneyMember>> GetActiveMembersAsync(Guid journeyId, CancellationToken cancellationToken = default);

    Task<JourneyMember> AddAsync(JourneyMember member, CancellationToken cancellationToken = default);

    Task<JourneyMember> UpdateAsync(JourneyMember member, CancellationToken cancellationToken = default);

    /// <summary>Tạo hoặc lấy thành viên owner (traveler = chủ journey).</summary>
    Task<JourneyMember> EnsureOwnerMemberAsync(
        Guid journeyId,
        Guid ownerTravelerId,
        string displayName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Chỉ **một** thành viên active và đó là **owner** (hoặc chưa có dòng member — legacy): luồng đi một mình, không bắt buộc rule “cả nhóm xác nhận đích” khi complete.
    /// Có 2+ người active hoặc chỉ còn member/guest: luồng multi-user.
    /// </summary>
    Task<bool> IsSoloOwnerOnlyActiveRosterAsync(Guid journeyId, CancellationToken cancellationToken = default);

    /// <summary>Mọi thành viên active đã xác nhận tới đích (check-in đích hoặc checkout đích hoặc skip đích). Waypoint không bắt buộc cho complete.</summary>
    Task<bool> AllActiveMembersConfirmedAtDestinationAsync(Guid journeyId, CancellationToken cancellationToken = default);

    Task<JourneyWaypointMemberProgress?> GetProgressAsync(
        Guid journeyMemberId,
        Guid? journeyWaypointId,
        string milestoneKind,
        CancellationToken cancellationToken = default);

    Task<JourneyWaypointMemberProgress> SaveProgressAsync(
        JourneyWaypointMemberProgress progress,
        CancellationToken cancellationToken = default);

    Task<List<JourneyWaypointMemberProgress>> GetProgressForMemberAsync(
        Guid journeyMemberId,
        CancellationToken cancellationToken = default);

    /// <summary>Tooltip x/N: N = số member active; x = đã tương tác waypoint (đến/skip).</summary>
    Task<JourneyWaypointAttendanceResponse> GetWaypointAttendanceAsync(
        Guid journeyId,
        CancellationToken cancellationToken = default);
}
