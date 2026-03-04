using JSEA_Application.DTOs.Respone.Weather;
using JSEA_Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JSEA_Presentation.Controllers;

[ApiController]
[Route("api/weather")]
public class WeatherController : ControllerBase
{
    private readonly IWeatherService _weatherService;

    public WeatherController(IWeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    /// <summary>
    /// Lấy thời tiết hiện tại theo tọa độ (Open-Meteo). Dùng latitude/longitude để gọi suggestions với tham số weather (Sunny/Cloudy/Rainy).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(CurrentWeatherResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetCurrent([FromQuery] double latitude, [FromQuery] double longitude, CancellationToken cancellationToken)
    {
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
            return BadRequest(new { message = "latitude phải từ -90 đến 90, longitude từ -180 đến 180." });

        var result = await _weatherService.GetCurrentWeatherAsync(latitude, longitude, cancellationToken);
        if (result == null)
            return StatusCode(502, new { message = "Không thể lấy thời tiết từ Open-Meteo. Thử lại sau." });

        return Ok(result);
    }
}
