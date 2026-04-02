using JSEA_Application.DTOs.Journey;

namespace JSEA_Application.Interfaces;

/// <summary>
/// Cache vị trí mới nhất của từng member trong journey (Redis / memory).
/// Key = journey:{journeyId}:member:{memberId}:loc — TTL tự hết hạn khi member ngừng gửi GPS.
/// </summary>
public interface IJourneyLocationCache
{
    /// <summary>Ghi vị trí mới nhất. TTL mặc định 5 phút (nếu hết thời gian không gửi, entry biến mất).</summary>
    Task SetAsync(JourneyMemberLocationNotification location, CancellationToken cancellationToken = default);

    /// <summary>Đọc vị trí mới nhất của 1 member.</summary>
    Task<JourneyMemberLocationNotification?> GetAsync(Guid journeyId, Guid memberId, CancellationToken cancellationToken = default);

    /// <summary>Đọc vị trí mới nhất tất cả member đang active (dùng khi client vừa mở map).</summary>
    Task<IReadOnlyList<JourneyMemberLocationNotification>> GetAllForJourneyAsync(Guid journeyId, IEnumerable<Guid> activeMemberIds, CancellationToken cancellationToken = default);

    /// <summary>Xóa entry (khi leave journey hoặc journey completed).</summary>
    Task RemoveAsync(Guid journeyId, Guid memberId, CancellationToken cancellationToken = default);
}
