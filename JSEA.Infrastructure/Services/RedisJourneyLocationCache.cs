using System.Text.Json;
using JSEA_Application.DTOs.Journey;
using JSEA_Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace JSEA_Infrastructure.Services;

public sealed class RedisJourneyLocationCache : IJourneyLocationCache
{
    private readonly IDistributedCache _cache;
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public RedisJourneyLocationCache(IDistributedCache cache)
    {
        _cache = cache;
    }

    private static string Key(Guid journeyId, Guid memberId) =>
        $"journey:{journeyId}:member:{memberId}:loc";

    public async Task SetAsync(JourneyMemberLocationNotification location, CancellationToken cancellationToken = default)
    {
        var key = Key(location.JourneyId, location.MemberId);
        var json = JsonSerializer.SerializeToUtf8Bytes(location, JsonOptions);
        await _cache.SetAsync(key, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = DefaultTtl
        }, cancellationToken);
    }

    public async Task<JourneyMemberLocationNotification?> GetAsync(
        Guid journeyId, Guid memberId, CancellationToken cancellationToken = default)
    {
        var bytes = await _cache.GetAsync(Key(journeyId, memberId), cancellationToken);
        if (bytes == null || bytes.Length == 0)
            return null;
        return JsonSerializer.Deserialize<JourneyMemberLocationNotification>(bytes, JsonOptions);
    }

    public async Task<IReadOnlyList<JourneyMemberLocationNotification>> GetAllForJourneyAsync(
        Guid journeyId, IEnumerable<Guid> activeMemberIds, CancellationToken cancellationToken = default)
    {
        var results = new List<JourneyMemberLocationNotification>();
        foreach (var memberId in activeMemberIds)
        {
            var loc = await GetAsync(journeyId, memberId, cancellationToken);
            if (loc != null)
                results.Add(loc);
        }
        return results;
    }

    public async Task RemoveAsync(Guid journeyId, Guid memberId, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(Key(journeyId, memberId), cancellationToken);
    }
}
