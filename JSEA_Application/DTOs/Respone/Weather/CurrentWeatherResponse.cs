using JSEA_Application.Enums;

namespace JSEA_Application.DTOs.Respone.Weather;

/// <summary>Thời tiết hiện tại từ Open-Meteo, đã map WMO code sang WeatherType (Sunny/Cloudy/Rainy) để lọc gợi ý.</summary>
public class CurrentWeatherResponse
{
    /// <summary>Loại thời tiết dùng cho filter micro-experience (Sunny, Cloudy, Rainy).</summary>
    public WeatherType WeatherType { get; set; }

    /// <summary>Mã WMO (0–99) từ Open-Meteo.</summary>
    public int WeatherCode { get; set; }

    /// <summary>Nhiệt độ 2m (°C).</summary>
    public double TemperatureC { get; set; }

    /// <summary>Vận tốc gió (km/h).</summary>
    public double WindSpeedKmh { get; set; }

    /// <summary>Thời điểm dữ liệu (ISO8601).</summary>
    public string? Time { get; set; }

    /// <summary>1 = ban ngày, 0 = ban đêm.</summary>
    public int IsDay { get; set; }
}
