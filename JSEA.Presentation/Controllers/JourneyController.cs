using JSEA_Application.DTOs.Request.Journey;
using JSEA_Application.DTOs.Respone.Journey;
using JSEA_Application.DTOs.Request.JourneyProgress;
using JSEA_Application.DTOs.Respone.JourneyProgress;
using JSEA_Application.Interfaces;
using JSEA_Application.Enums;
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
    private readonly IJourneyProgressService _journeyProgressService;
    private readonly IJourneyShareService _journeyShareService;

    public JourneyController(
        IJourneyService journeyService,
        ISuggestService suggestService,
        IJourneyProgressService journeyProgressService,
        IJourneyShareService journeyShareService)
    {
        _journeyService = journeyService;
        _suggestService = suggestService;
        _journeyProgressService = journeyProgressService;
        _journeyShareService = journeyShareService;
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

    [HttpGet("shared/{shareCode}")]
    [ProducesResponseType(typeof(PublicSharedJourneyDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSharedJourney(string shareCode, CancellationToken cancellationToken)
    {
        var result = await _journeyShareService.GetPublicDetailByShareCodeAsync(shareCode, cancellationToken);
        if (result == null)
            return NotFound(new { message = "Không tìm thấy link chia sẻ." });
        return Ok(result);
    }

    [HttpGet("shared")]
    [ProducesResponseType(typeof(List<PublicSharedJourneyListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicSharedJourneys(
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var list = await _journeyShareService.GetPublicSharedJourneysAsync(page, pageSize, cancellationToken);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(JourneyDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        Guid? viewerId = null;
        if (User?.Identity?.IsAuthenticated == true &&
            Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid))
            viewerId = uid;

        var detail = await _journeyService.GetByIdAsync(id, viewerId, cancellationToken);
        if (detail == null)
            return NotFound(new { message = "Không tìm thấy hành trình." });
        return Ok(detail);
    }

    /// <summary>
    /// Lấy polyline tuyến đi qua các waypoint đã chọn (để FE vẽ map). (Authorized)
    /// </summary>
    [HttpGet("{journeyId:guid}/polyline")]
    [Authorize]
    [ProducesResponseType(typeof(JourneyPolylineResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetJourneyPolyline(
        Guid journeyId,
        CancellationToken cancellationToken,
        [FromQuery] double? latitude,
        [FromQuery] double? longitude,
        [FromQuery] bool excludeCompletedWaypoints = true)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var travelerId))
            return Unauthorized(new { message = "Vui lòng đăng nhập." });

        if ((latitude.HasValue && !longitude.HasValue) || (!latitude.HasValue && longitude.HasValue))
            return BadRequest(new { message = "Vui lòng truyền đủ latitude và longitude." });

        try
        {
            JourneyPolylineResponse? polyline;
            if (latitude.HasValue && longitude.HasValue)
            {
                polyline = await _journeyService.GetNearestWaypointPolylineAsync(
                    journeyId,
                    travelerId,
                    latitude.Value,
                    longitude.Value,
                    excludeCompletedWaypoints,
                    cancellationToken);
            }
            else
            {
                polyline = await _journeyService.GetJourneyPolylineAsync(journeyId, travelerId, cancellationToken);
            }

            if (polyline == null)
                return StatusCode(502, new { message = "Không thể lấy polyline (kiểm tra waypoints/tọa độ hoặc API Goong Maps)." });

            return Ok(polyline);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            // Tránh leak existence
            return NotFound(new { message = "Không tìm thấy hành trình." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Thiết lập hành trình: điểm đi, điểm đến, loại xe, thời gian, độ lệch, travel vibe, thời gian dừng ưu tiên. (Authorized)
    /// </summary>
    [HttpPost("setup")]
    [Authorize]
    [ProducesResponseType(typeof(JourneySetupResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> SetupJourney(
        [FromBody] JourneySetupRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || !ModelState.IsValid)
            return BadRequest(ModelState);

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var travelerId))
            return Unauthorized(new { message = "Vui lòng đăng nhập." });

        var result = await _journeyService.ValidateAndCreateJourneyAsync(request, travelerId, cancellationToken);
        if (result == null)
            return StatusCode(502, new { message = "Không thể phân tích tuyến. Kiểm tra tọa độ hoặc API Goong Maps." });

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

    /// <summary>
    /// Khi xem chi tiết gợi ý: metrics địa điểm (experience_metrics) + feedback đã duyệt từ người khác (có sao nếu có rating).
    /// Chỉ chủ journey của suggestion này mới gọi được.
    /// </summary>
    [HttpGet("suggestions/{suggestionId:guid}/community")]
    [Authorize]
    [ProducesResponseType(typeof(SuggestionCommunityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSuggestionCommunity(
        Guid suggestionId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var travelerId))
            return Unauthorized(new { message = "Vui lòng đăng nhập." });

        var result = await _suggestService.GetSuggestionCommunityAsync(
            suggestionId,
            travelerId,
            page,
            pageSize,
            cancellationToken);

        if (result == null)
            return NotFound(new { message = "Không tìm thấy gợi ý hoặc bạn không có quyền xem." });

        return Ok(result);
    }

    /// <summary>
    /// User chọn các điểm muốn ghé (waypoints) sau khi xem suggestions.
    /// Replace toàn bộ waypoint hiện tại của journey.
    /// </summary>
    [HttpPut("{journeyId:guid}/waypoints")]
    [Authorize]
    [ProducesResponseType(typeof(SaveWaypointsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveWaypoints(
        Guid journeyId,
        [FromBody] SaveWaypointsRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || !ModelState.IsValid)
            return BadRequest(ModelState);

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var travelerId))
            return Unauthorized(new { message = "Vui lòng đăng nhập." });

        var ok = await _journeyService.SaveSelectedWaypointsAsync(
            journeyId,
            travelerId,
            request.SegmentId,
            request.Waypoints,
            cancellationToken);

        if (!ok)
            return BadRequest(new { message = "Không thể lưu waypoints (kiểm tra route/đề xuất/time budget)." });

        // Additive response: keep message, also return waypointId list for FE check-in/checkout.
        var detail = await _journeyService.GetByIdAsync(journeyId, travelerId, cancellationToken);

        return Ok(new SaveWaypointsResponse
        {
            Message = "Đã lưu waypoints.",
            Waypoints = detail?.Waypoints
                ?.OrderBy(w => w.StopOrder)
                .Select(w => new SavedWaypointItemResponse
                {
                    WaypointId = w.WaypointId,
                    ExperienceId = w.ExperienceId,
                    SuggestionId = w.SuggestionId,
                    StopOrder = w.StopOrder
                })
                .ToList()
        });
    }

    /// <summary>
    /// Cập nhật mood trong giai đoạn planning (trước khi lưu waypoints).
    /// Khi đổi mood, backend sẽ xóa cache suggestions để lần gọi suggest kế tiếp regenerate theo mood mới.
    /// </summary>
    [HttpPut("{journeyId:guid}/mood")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMood(
        Guid journeyId,
        [FromBody] UpdateJourneyMoodRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || !ModelState.IsValid)
            return BadRequest(ModelState);

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var travelerId))
            return Unauthorized(new { message = "Vui lòng đăng nhập." });

        var ok = await _journeyService.UpdateCurrentMoodAsync(
            journeyId,
            travelerId,
            request.CurrentMood,
            cancellationToken);

        if (!ok)
            return BadRequest(new { message = "Không thể cập nhật mood (chỉ cho phép khi đang planning và chưa lưu waypoints)." });

        return Ok(new { message = "Đã cập nhật mood.", currentMood = request.CurrentMood });
    }

    /// <summary>
    /// Bắt đầu hành trình (FE bấm "Start journey"). Set StartedAt và chuyển status sang InProgress. (Authorized)
    /// </summary>
    [HttpPost("{journeyId:guid}/start")]
    [Authorize]
    [ProducesResponseType(typeof(StartJourneyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartJourney(Guid journeyId, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var travelerId))
            return Unauthorized(new { message = "Vui lòng đăng nhập." });

        var result = await _journeyProgressService.StartJourneyAsync(journeyId, travelerId, cancellationToken);
        if (result == null)
            return NotFound(new { message = "Không tìm thấy hành trình." });

        return Ok(result);
    }

    /// <summary>
    /// Hoàn tất hành trình (FE bấm "Complete journey"). Set CompletedAt và chuyển status sang Completed. (Authorized)
    /// </summary>
    [HttpPost("{journeyId:guid}/complete")]
    [Authorize]
    [ProducesResponseType(typeof(CompleteJourneyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteJourney(Guid journeyId, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var travelerId))
            return Unauthorized(new { message = "Vui lòng đăng nhập." });

        try
        {
            var result = await _journeyProgressService.CompleteJourneyAsync(journeyId, travelerId, cancellationToken);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy hành trình." });

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Check-in tại waypoint (FE bấm "Ghé thăm"). Tạo Visit; Feedback optional. (Authorized)
    /// </summary>
    [HttpPost("{journeyId:guid}/waypoints/{waypointId:guid}/checkin")]
    [Authorize]
    [ProducesResponseType(typeof(WaypointCheckInResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckIn(
        Guid journeyId,
        Guid waypointId,
        [FromBody] WaypointCheckInRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || !ModelState.IsValid)
            return BadRequest(ModelState);

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var travelerId))
            return Unauthorized(new { message = "Vui lòng đăng nhập." });

        var result = await _journeyProgressService.CheckInAsync(journeyId, waypointId, travelerId, request, cancellationToken);
        if (result == null)
            return NotFound(new { message = "Không tìm thấy waypoint hoặc hành trình chưa bắt đầu." });

        return Ok(result);
    }

    /// <summary>
    /// Check-out (FE bấm "Rời đi"). Rating tuỳ chọn (1–5); bỏ trống thì không ghi rating. (Authorized)
    /// </summary>
    [HttpPost("{journeyId:guid}/waypoints/{waypointId:guid}/checkout")]
    [Authorize]
    [ProducesResponseType(typeof(WaypointCheckOutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckOut(
        Guid journeyId,
        Guid waypointId,
        [FromBody] WaypointCheckOutRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || !ModelState.IsValid)
            return BadRequest(ModelState);

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var travelerId))
            return Unauthorized(new { message = "Vui lòng đăng nhập." });

        var result = await _journeyProgressService.CheckOutAsync(journeyId, waypointId, travelerId, request, cancellationToken);
        if (result == null)
            return BadRequest(new { message = "Không thể check-out (rating 1–5 nếu gửi, hoặc hành trình/waypoint không tồn tại/không hợp lệ)." });

        return Ok(result);
    }

    /// <summary>
    /// Skip một waypoint (user bấm "Skip").
    /// Backend sẽ đánh dấu waypoint là completed (ActualDepartureAt != null) để tuyến tiếp theo bỏ qua điểm này.
    /// Trả về polyline mới từ vị trí hiện tại tới waypoint kế tiếp (hoặc destination nếu hết waypoint).
    /// </summary>
    [HttpPost("{journeyId:guid}/waypoints/{waypointId:guid}/skip")]
    [Authorize]
    [ProducesResponseType(typeof(JourneyPolylineResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> SkipWaypoint(
        Guid journeyId,
        Guid waypointId,
        CancellationToken cancellationToken,
        [FromQuery] double latitude,
        [FromQuery] double longitude)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var travelerId))
            return Unauthorized(new { message = "Vui lòng đăng nhập." });

        if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
            return BadRequest(new { message = "Tọa độ không hợp lệ." });

        var skipped = await _journeyProgressService.SkipWaypointAsync(journeyId, waypointId, travelerId, cancellationToken);
        if (skipped == null)
            return NotFound(new { message = "Không tìm thấy waypoint hoặc hành trình chưa bắt đầu." });

        try
        {
            var polyline = await _journeyService.GetNearestWaypointPolylineAsync(
                journeyId,
                travelerId,
                latitude,
                longitude,
                excludeCompletedWaypoints: true,
                cancellationToken);

            if (polyline == null)
                return StatusCode(502, new { message = "Không thể lấy polyline tiếp theo (kiểm tra API Goong Maps)." });

            return Ok(polyline);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound(new { message = "Không tìm thấy hành trình." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Log interaction của user với suggestion (ViewedDetails/Saved/Accepted/Skipped...).
    /// </summary>
    [HttpPost("suggestions/{suggestionId:guid}/interactions")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogSuggestionInteraction(
        Guid suggestionId,
        [FromBody] LogSuggestionInteractionRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || !ModelState.IsValid)
            return BadRequest(ModelState);

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var travelerId))
            return Unauthorized(new { message = "Vui lòng đăng nhập." });

        var ok = await _journeyService.LogSuggestionInteractionAsync(
            suggestionId,
            travelerId,
            request.InteractionType,
            cancellationToken);

        if (!ok)
            return BadRequest(new { message = "Không thể ghi nhận interaction." });

        return Ok(new { message = "Đã ghi nhận interaction." });
    }

    [HttpPost("{journeyId:guid}/share")]
    [Authorize]
    [ProducesResponseType(typeof(ShareJourneyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ShareJourney(Guid journeyId, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var travelerId))
            return Unauthorized(new { message = "Vui lòng đăng nhập." });

        var result = await _journeyShareService.ShareJourneyAsync(journeyId, travelerId, cancellationToken);
        if (result == null)
            return NotFound(new { message = "Không tìm thấy hành trình." });

        return Ok(result);
    }

    [HttpPut("{journeyId:guid}/journey-feedback")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateJourneyFeedback(
        Guid journeyId,
        [FromBody] UpdateJourneyFeedbackRequest? request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var travelerId))
            return Unauthorized(new { message = "Vui lòng đăng nhập." });

        var ok = await _journeyService.UpdateJourneyFeedbackAsync(
            journeyId,
            travelerId,
            request?.JourneyFeedback,
            cancellationToken);

        if (!ok)
            return NotFound(new { message = "Không tìm thấy hành trình." });

        return Ok(new { message = "Đã cập nhật feedback." });
    }
}
