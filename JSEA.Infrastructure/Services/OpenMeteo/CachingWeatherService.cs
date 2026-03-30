using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using JSEA_Application.DTOs.Respone.Weather;
using JSEA_Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JSEA_Infrastructure.Services.OpenMeteo;

/// <summary>
/// Bọc <see cref="OpenMeteoWeatherService"/>: đọc/ghi <see cref="CurrentWeatherResponse"/> qua <see cref="IDistributedCache"/> (Redis hoặc memory).
/// Làm tròn tọa độ để gom request gần nhau về cùng một key, giảm số lần gọi Open-Meteo.
/// </summary>
public sealed class CachingWeatherService : IWeatherService
{
    private const string KeyPrefix = "weather:current:";
    private readonly OpenMeteoWeatherService _inner;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachingWeatherService> _logger;
    private readonly TimeSpan _ttl;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        WriteIndented = false
    };

    public CachingWeatherService(
        OpenMeteoWeatherService inner,
        IDistributedCache cache,
        IOptions<WeatherCacheOptions> options,
        ILogger<CachingWeatherService> logger)
    {
        _inner = inner;
        _cache = cache;
        _logger = logger;
        var minutes = Math.Max(1, options.Value.AbsoluteExpirationMinutes);
        _ttl = TimeSpan.FromMinutes(minutes);
    }

    public async Task<CurrentWeatherResponse?> GetCurrentWeatherAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        var key = BuildCacheKey(latitude, longitude);

        try
        {
            var cached = await _cache.GetAsync(key, cancellationToken).ConfigureAwait(false);
            if (cached is { Length: > 0 })
            {
                var dto = JsonSerializer.Deserialize<CurrentWeatherResponse>(cached, SerializerOptions);
                if (dto is not null)
                    return dto;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Weather cache read failed for key {Key}, calling Open-Meteo directly.", key);
        }

        var fresh = await _inner.GetCurrentWeatherAsync(latitude, longitude, cancellationToken).ConfigureAwait(false);
        if (fresh is null)
            return null;

        try
        {
            var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(fresh, SerializerOptions));
            await _cache.SetAsync(
                    key,
                    bytes,
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _ttl },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Weather cache write failed for key {Key}; response vẫn trả về từ Open-Meteo.", key);
        }

        return fresh;
    }

    /// <summary>Làm tròn 3 chữ thập phân (~hàng trăm mét) để nhiều request gần vị trí dùng chung cache.</summary>
    internal static string BuildCacheKey(double latitude, double longitude)
    {
        var lat = Math.Round(latitude, 3, MidpointRounding.AwayFromZero);
        var lon = Math.Round(longitude, 3, MidpointRounding.AwayFromZero);
        return $"{KeyPrefix}{lat.ToString(CultureInfo.InvariantCulture)},{lon.ToString(CultureInfo.InvariantCulture)}";
    }
}
