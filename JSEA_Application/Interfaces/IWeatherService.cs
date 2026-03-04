using JSEA_Application.DTOs.Respone.Weather;

namespace JSEA_Application.Interfaces;

/// <summary>Lấy thời tiết hiện tại theo tọa độ (Open-Meteo).</summary>
public interface IWeatherService
{
    /// <summary>Lấy thời tiết hiện tại tại (latitude, longitude). Trả về null nếu gọi API lỗi.</summary>
    Task<CurrentWeatherResponse?> GetCurrentWeatherAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
}
