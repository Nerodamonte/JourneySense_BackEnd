using System.Collections.Concurrent;

namespace JSEA_Presentation.Services;

/// <summary>Giới hạn tần suất cập nhật GPS trên hub journey-live theo từng journey member (mặc định ~750ms).</summary>
public sealed class JourneyLiveLocationRateLimiter
{
    public const int DefaultMinIntervalMs = 750;

    private readonly ConcurrentDictionary<Guid, long> _lastUtcMs = new();

    /// <returns><c>true</c> nếu được phép broadcast vị trí lần này.</returns>
    public bool TryAllow(Guid journeyMemberId, int minIntervalMs = DefaultMinIntervalMs)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var stored = _lastUtcMs.AddOrUpdate(
            journeyMemberId,
            now,
            (_, old) => now - old >= minIntervalMs ? now : old);
        return stored == now;
    }
}
