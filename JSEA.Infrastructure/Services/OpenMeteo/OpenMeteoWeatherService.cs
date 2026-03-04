using System.Text.Json;
using JSEA_Application.DTOs.Respone.Weather;
using JSEA_Application.Enums;
using JSEA_Application.Interfaces;

namespace JSEA_Infrastructure.Services.OpenMeteo;

public class OpenMeteoWeatherService : IWeatherService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private const string BaseUrl = "https://api.open-meteo.com/v1/forecast";

    public OpenMeteoWeatherService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<CurrentWeatherResponse?> GetCurrentWeatherAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"{BaseUrl}?latitude={latitude:G}&longitude={longitude:G}&current_weather=true";
        var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = doc.RootElement;
        if (!root.TryGetProperty("current_weather", out var current))
            return null;

        var weatherCode = current.TryGetProperty("weathercode", out var wc) ? wc.GetInt32() : 0;
        var temperature = current.TryGetProperty("temperature", out var t) ? t.GetDouble() : 0;
        var windSpeed = current.TryGetProperty("windspeed", out var ws) ? ws.GetDouble() : 0;
        var time = current.TryGetProperty("time", out var ts) ? ts.GetString() : null;
        var isDay = current.TryGetProperty("is_day", out var id) ? id.GetInt32() : 1;

        var weatherType = MapWmoCodeToWeatherType(weatherCode);

        return new CurrentWeatherResponse
        {
            WeatherType = weatherType,
            WeatherCode = weatherCode,
            TemperatureC = temperature,
            WindSpeedKmh = windSpeed,
            Time = time,
            IsDay = isDay
        };
    }

    /// <summary>Map WMO weather code (0–99) to Sunny / Cloudy / Rainy.</summary>
    private static WeatherType MapWmoCodeToWeatherType(int code)
    {
        if (code == 0) return WeatherType.Sunny;
        if (code is >= 1 and <= 3 || code is 45 or 48) return WeatherType.Cloudy;
        return WeatherType.Rainy; // 51–99: drizzle, rain, snow, thunderstorm
    }
}
