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
    private readonly ISuggestService _suggestService;

    public JourneyController(IJourneyService journeyService, ISuggestService suggestService)
    {
        _journeyService = journeyService;
        _suggestService = suggestService;
    }

    /// <summary>
    /// Lấy danh sách hành trình của user đăng nhập (lịch sử micro-journey). (Authorized)
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
    /// Thiết lập hành trình: điểm đi, điểm đến, loại xe, thời gian, độ lệch, travel vibe, thời gian dừng ưu tiên. (Authorized)
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

    /// <summary>
    /// Pipeline gợi ý v11. Gọi khi GPS user vào gần segment.
    /// Hard filter → Gemini embed → cosine search → score → INSERT suggestions.
    /// </summary>
    [HttpPost("{journeyId:guid}/segments/{segmentId:guid}/suggest")]
    [Authorize]
    [ProducesResponseType(typeof(List<SuggestionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSuggestions(
        Guid journeyId,
        Guid segmentId,
        CancellationToken cancellationToken)
    {
        var results = await _suggestService.GetSuggestionsAsync(journeyId, segmentId, cancellationToken);
        return Ok(results);
    }

    /// <summary>
    /// Tạo AI insight cho một suggestion (RAG). Gọi khi user tap vào suggestion.
    /// Nếu insight đã có thì trả về luôn, không gọi Gemini lại.
    /// </summary>
    [HttpPost("suggestions/{suggestionId:guid}/insight")]
    [Authorize]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAiInsight(
        Guid suggestionId,
        CancellationToken cancellationToken)
    {
        var insight = await _suggestService.GetAiInsightAsync(suggestionId, cancellationToken);
        if (insight == null)
            return NotFound(new { message = "Không tìm thấy gợi ý hoặc không thể tạo insight." });
        return Ok(new { insight });
    }
}
