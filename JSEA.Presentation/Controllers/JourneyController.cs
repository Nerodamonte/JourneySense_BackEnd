using JSEA_Application.DTOs.Request.Journey;
using JSEA_Application.DTOs.Respone.Journey;
using JSEA_Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JSEA_Presentation.Controllers;

[ApiController]
[Route("api/journeys")]
public class JourneyController : ControllerBase
{
    private readonly IJourneyService _journeyService;

    public JourneyController(IJourneyService journeyService)
    {
        _journeyService = journeyService;
    }

    /// <summary>
    /// Lấy danh sách hành trình của user đăng nhập (lịch sử micro-journey). Yêu cầu đăng nhập.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(List<JourneyListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyJourneys(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var travelerId))
            return Unauthorized(new { message = "Vui lòng đăng nhập." });

        var list = await _journeyService.GetMyJourneysAsync(travelerId, cancellationToken);
        return Ok(list);
    }

    /// <summary>
    /// Lấy chi tiết một hành trình theo id.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(JourneyDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var detail = await _journeyService.GetByIdAsync(id, cancellationToken);
        if (detail == null)
            return NotFound(new { message = "Không tìm thấy hành trình." });
        return Ok(detail);
    }

    /// <summary>
    /// Gợi ý micro-experiences dọc/gần tuyến: lọc theo vibe (current_mood) của journey, weather, timeOfDay, trong bán kính detour. Query: limit (1–50), weather (Sunny/Cloudy/Rainy), timeOfDay (Morning/Afternoon/Evening/Night).
    /// </summary>
    [HttpGet("{id:guid}/suggestions")]
    [ProducesResponseType(typeof(List<RouteMicroExperienceSuggestionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSuggestionsAlongRoute(
        Guid id,
        [FromQuery] int? limit,
        [FromQuery] JSEA_Application.Enums.WeatherType? weather,
        [FromQuery] JSEA_Application.Enums.TimeOfDay? timeOfDay,
        CancellationToken cancellationToken)
    {
        var list = await _journeyService.GetSuggestionsAlongRouteAsync(id, limit, weather, timeOfDay, cancellationToken);
        return Ok(list);
    }

    /// <summary>
    /// Thiết lập hành trình: điểm đi, điểm đến, loại xe, thời gian, độ lệch, travel vibe, thời gian dừng ưu tiên. Gọi Goong Maps phân tích tuyến và lưu journey + waypoints.
    /// </summary>
    [HttpPost("setup")]
    [ProducesResponseType(typeof(JourneySetupResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> SetupJourney(
        [FromBody] JourneySetupRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || !ModelState.IsValid)
            return BadRequest(ModelState);

        Guid? travelerId = null;
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdClaim, out var uid))
            travelerId = uid;

        var result = await _journeyService.ValidateAndCreateJourneyAsync(request, travelerId, cancellationToken);
        if (result == null)
            return StatusCode(502, new { message = "Không thể phân tích tuyến. Kiểm tra địa chỉ hoặc API Goong Maps." });

        return Created($"/api/journeys/{result.JourneyId}", result);
    }
}
