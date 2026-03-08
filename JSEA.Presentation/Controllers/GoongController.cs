using JSEA_Application.DTOs.Respone.Journey;
using JSEA_Application.DTOs.Respone.Place;
using JSEA_Application.Enums;
using JSEA_Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JSEA_Presentation.Controllers;

[ApiController]
[Route("api/goong")]
public class GoongController : ControllerBase
{
    private readonly IGoongMapsService _goongMapsService;

    public GoongController(IGoongMapsService goongMapsService)
    {
        _goongMapsService = goongMapsService;
    }

    /// <summary>
    /// Gợi ý địa điểm theo từ khóa (Goong Place AutoComplete). Dùng cho ô tìm kiếm địa chỉ khi chọn điểm đi/điểm đến.
    /// </summary>
    [HttpGet("place-suggestions")]
    [ProducesResponseType(typeof(List<PlaceSuggestionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPlaceSuggestions(
        [FromQuery] string input,
        [FromQuery] double? latitude,
        [FromQuery] double? longitude,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
            return BadRequest(new { message = "input không được để trống." });
        if (limit is < 1 or > 20)
            limit = 10;

        var list = await _goongMapsService.SearchPlaceSuggestionsAsync(input, latitude, longitude, limit, cancellationToken);
        return Ok(list);
    }

    /// <summary>
    /// Lấy chi tiết địa điểm theo place_id (từ Place AutoComplete). Trả về tên, địa chỉ đầy đủ, tọa độ.
    /// </summary>
    [HttpGet("place-detail")]
    [ProducesResponseType(typeof(PlaceDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlaceDetail(
        [FromQuery] string placeId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(placeId))
            return BadRequest(new { message = "placeId không được để trống." });

        var detail = await _goongMapsService.GetPlaceDetailAsync(placeId, cancellationToken);
        if (detail == null)
            return NotFound(new { message = "Không tìm thấy chi tiết địa điểm cho place_id này." });

        return Ok(detail);
    }

    /// <summary>
    /// Test Goong Geocode: trả về toạ độ (lat,lng) cho một địa chỉ text.
    /// Dùng để kiểm tra API key và geocode.
    /// </summary>
    [HttpGet("geocode")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TestGeocode(
        [FromQuery] string address,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(address))
            return BadRequest(new { message = "address không được để trống." });

        var point = await _goongMapsService.GeocodeAddressToPointAsync(address, cancellationToken);
        if (point == null)
            return Ok(new { success = false, message = "Goong không geocode được địa chỉ này." });

        return Ok(new
        {
            success = true,
            address,
            latitude = point.Y,
            longitude = point.X
        });
    }

    /// <summary>
    /// Test Goong Direction: phân tích tuyến từ originAddress → destinationAddress với vehicleType.
    /// Trả về danh sách các route (distance, duration, polyline).
    /// </summary>
    [HttpGet("route")]
    [ProducesResponseType(typeof(List<JSEA_Application.DTOs.Respone.Journey.RouteContext>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TestRoute(
        [FromQuery] string originAddress,
        [FromQuery] string destinationAddress,
        [FromQuery] VehicleType vehicleType = VehicleType.Walking,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(originAddress) || string.IsNullOrWhiteSpace(destinationAddress))
            return BadRequest(new { message = "originAddress và destinationAddress không được để trống." });

        var routes = await _goongMapsService.AnalyzeRouteContextAsync(
            originAddress,
            destinationAddress,
            vehicleType,
            timeBudgetMinutes: 60,
            maxDetourDistanceMeters: 2000,
            cancellationToken);

        if (routes == null || routes.Count == 0)
            return Ok(new { success = false, message = "Goong không trả về route nào cho cặp địa chỉ + vehicleType này." });

        return Ok(new
        {
            success = true,
            originAddress,
            destinationAddress,
            vehicleType,
            routeCount = routes.Count,
            routes = routes.Select(r => new
            {
                r.TotalDistanceMeters,
                r.EstimatedDurationMinutes,
                r.OriginLatitude,
                r.OriginLongitude,
                r.DestinationLatitude,
                r.DestinationLongitude,
                r.Polyline,
                r.ExperienceCount
            })
        });
    }
}

