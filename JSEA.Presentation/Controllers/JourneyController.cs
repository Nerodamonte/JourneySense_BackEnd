using JSEA_Application.DTOs.Journey;
using JSEA_Application.DTOs.Request.Journey;
using JSEA_Application.DTOs.Respone.Journey;
using JSEA_Application.DTOs.Request.JourneyProgress;
using JSEA_Application.DTOs.Respone.JourneyProgress;
using JSEA_Application.Interfaces;
using JSEA_Application.Enums;
using JSEA_Presentation.Services;
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
    private readonly IJourneyLocationCache _locationCache;
    private readonly IJourneyLiveNotifier _liveNotifier;
    private readonly IJourneyMemberRepository _memberRepo;
    private readonly JourneyLiveLocationRateLimiter _rateLimiter;

    public JourneyController(
        IJourneyService journeyService,
        ISuggestService suggestService,
        IJourneyProgressService journeyProgressService,
        IJourneyShareService journeyShareService,
        IJourneyLocationCache locationCache,
        IJourneyLiveNotifier liveNotifier,
        IJourneyMemberRepository memberRepo,
        JourneyLiveLocationRateLimiter rateLimiter)
    {
        _journeyService = journeyService;
        _suggestService = suggestService;
        _journeyProgressService = journeyProgressService;
        _journeyShareService = journeyShareService;
        _locationCache = locationCache;
        _liveNotifier = liveNotifier;
        _memberRepo = memberRepo;
        _rateLimiter = rateLimiter;
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
    /// Lấy polyline tuyến đi qua các waypoint đã chọn (để FE vẽ map). JWT hoặc <c>guestKey</c> (member đã join).
    /// </summary>
    [HttpGet("{journeyId:guid}/polyline")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(JourneyPolylineResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetJourneyPolyline(
        Guid journeyId,
        CancellationToken cancellationToken,
        [FromQuery] Guid? guestKey,
        [FromQuery] double? latitude,
        [FromQuery] double? longitude,
        [FromQuery] bool excludeCompletedWaypoints = true)
    {
        Guid? travelerId = null;
        if (User?.Identity?.IsAuthenticated == true &&
            Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var tid))
            travelerId = tid;

        if (!travelerId.HasValue && !guestKey.HasValue)
            return Unauthorized(new { message = "Cần đăng nhập hoặc guestKey của thành viên đã join." });

        if (travelerId.HasValue)
            guestKey = null;

        if ((latitude.HasValue && !longitude.HasValue) || (!latitude.HasValue && longitude.HasValue))
            return BadRequest(new { message = "Vui lòng truyền đủ latitude và longitude." });

        try
        {
            JourneyPolylineResponse? polyline;
            if (latitude.HasValue && longitude.HasValue)
            {
                polyline = travelerId.HasValue
                    ? await _journeyService.GetNearestWaypointPolylineAsync(
                        journeyId,
                        travelerId.Value,
                        latitude.Value,
                        longitude.Value,
                        excludeCompletedWaypoints,
                        cancellationToken)
                    : await _journeyService.GetNearestWaypointPolylineForGuestAsync(
                        journeyId,
                        guestKey!.Value,
                        latitude.Value,
                        longitude.Value,
                        excludeCompletedWaypoints,
                        cancellationToken);
            }
            else
            {
                polyline = travelerId.HasValue
                    ? await _journeyService.GetJourneyPolylineAsync(journeyId, travelerId.Value, cancellationToken)
                    : await _journeyService.GetJourneyPolylineForGuestAsync(journeyId, guestKey!.Value, cancellationToken);
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
    /// Tooltip x/N tại từng waypoint: N = số thành viên active; arrivedCount = đã check-in/out hoặc skip. JWT hoặc <c>guestKey</c>.
    /// </summary>
    [HttpGet("{journeyId:guid}/waypoints/attendance")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(JourneyWaypointAttendanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWaypointAttendance(
        Guid journeyId,
        CancellationToken cancellationToken,
        [FromQuery] Guid? guestKey = null)
    {
        Guid? travelerId = null;
        if (User?.Identity?.IsAuthenticated == true &&
            Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var tid))
            travelerId = tid;

        if (!travelerId.HasValue && !guestKey.HasValue)
            return Unauthorized(new { message = "Cần đăng nhập hoặc guestKey của thành viên đã join." });

        if (travelerId.HasValue)
            guestKey = null;

        try
        {
            var result = travelerId.HasValue
                ? await _journeyService.GetWaypointAttendanceAsync(journeyId, travelerId.Value, cancellationToken)
                : await _journeyService.GetWaypointAttendanceForGuestAsync(journeyId, guestKey!.Value, cancellationToken);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy hành trình." });

            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound(new { message = "Không tìm thấy hành trình." });
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

        try
        {
            var result = await _journeyService.ValidateAndCreateJourneyAsync(request, travelerId, cancellationToken);
            if (result == null)
                return StatusCode(502, new { message = "Không thể phân tích tuyến. Kiểm tra tọa độ hoặc API Goong Maps." });

            return Created($"/api/journeys/{result.JourneyId}", result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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

        try
        {
            var ok = await _journeyService.SaveSelectedWaypointsAsync(
                journeyId,
                travelerId,
                request.SegmentId,
                request.Waypoints,
                cancellationToken);

            if (!ok)
                return BadRequest(new { message = "Không thể lưu waypoints (kiểm tra route/đề xuất/hạn km gói)." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

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

    /// <summary>Gia nhập hành trình qua mã chia sẻ (user đã đăng nhập).</summary>
    [HttpPost("shared/{shareCode}/join")]
    [Authorize]
    [ProducesResponseType(typeof(JoinJourneyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> JoinSharedJourney(string shareCode, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var travelerId))
            return Unauthorized(new { message = "Vui lòng đăng nhập." });

        var result = await _journeyShareService.JoinByShareCodeAsync(shareCode, travelerId, cancellationToken);
        if (result == null)
            return NotFound(new { message = "Không thể tham gia (link không hợp lệ hoặc hành trình đã kết thúc)." });

        return Ok(result);
    }

    /// <summary>Gia nhập với tên hiển thị (khách, không đăng nhập).</summary>
    [HttpPost("shared/{shareCode}/join-guest")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(JoinJourneyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> JoinSharedJourneyAsGuest(
        string shareCode,
        [FromBody] JoinJourneyGuestRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || !ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _journeyShareService.JoinGuestByShareCodeAsync(shareCode, request, cancellationToken);
        if (result == null)
            return NotFound(new { message = "Không thể tham gia (link không hợp lệ hoặc hành trình đã kết thúc)." });

        return Ok(result);
    }

    /// <summary>Rời khỏi hành trình với tư cách thành viên (không áp dụng owner).</summary>
    [HttpPost("{journeyId:guid}/leave")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LeaveJourney(Guid journeyId, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var travelerId))
            return Unauthorized(new { message = "Vui lòng đăng nhập." });

        var ok = await _journeyShareService.LeaveJourneyAsync(journeyId, travelerId, cancellationToken);
        if (!ok)
            return BadRequest(new { message = "Không thể rời đi (bạn không phải thành viên hoặc là chủ hành trình)." });

        return Ok(new { message = "Đã rời khỏi hành trình." });
    }

    [HttpPost("{journeyId:guid}/leave-guest")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LeaveJourneyAsGuest(
        Guid journeyId,
        [FromBody] GuestKeyBodyRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || !ModelState.IsValid)
            return BadRequest(ModelState);

        var ok = await _journeyShareService.LeaveJourneyGuestAsync(journeyId, request.GuestKey, cancellationToken);
        if (!ok)
            return BadRequest(new { message = "Không thể rời đi (không tìm thấy khách hoặc là chủ hành trình)." });

        return Ok(new { message = "Đã rời khỏi hành trình." });
    }

    [HttpPost("{journeyId:guid}/destination/checkin")]
    [Authorize]
    [ProducesResponseType(typeof(DestinationCheckpointResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DestinationCheckIn(Guid journeyId, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var travelerId))
            return Unauthorized(new { message = "Vui lòng đăng nhập." });

        var result = await _journeyProgressService.DestinationCheckInAsync(journeyId, travelerId, cancellationToken);
        if (result == null)
            return NotFound(new { message = "Không tìm thấy hành trình hoặc bạn chưa tham gia / hành trình chưa bắt đầu." });

        return Ok(result);
    }

    [HttpPost("{journeyId:guid}/destination/checkout")]
    [Authorize]
    [ProducesResponseType(typeof(DestinationCheckpointResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DestinationCheckOut(Guid journeyId, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var travelerId))
            return Unauthorized(new { message = "Vui lòng đăng nhập." });

        var result = await _journeyProgressService.DestinationCheckOutAsync(journeyId, travelerId, cancellationToken);
        if (result == null)
            return NotFound(new { message = "Không tìm thấy hành trình hoặc bạn chưa tham gia / hành trình chưa bắt đầu." });

        return Ok(result);
    }

    [HttpPost("{journeyId:guid}/destination/checkin-guest")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DestinationCheckpointResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DestinationCheckInGuest(
        Guid journeyId,
        [FromBody] GuestKeyBodyRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || !ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _journeyProgressService.DestinationCheckInGuestAsync(journeyId, request.GuestKey, cancellationToken);
        if (result == null)
            return NotFound(new { message = "Không tìm thấy hành trình hoặc guest key không hợp lệ / hành trình chưa bắt đầu." });

        return Ok(result);
    }

    [HttpPost("{journeyId:guid}/destination/checkout-guest")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DestinationCheckpointResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DestinationCheckOutGuest(
        Guid journeyId,
        [FromBody] GuestKeyBodyRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || !ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _journeyProgressService.DestinationCheckOutGuestAsync(journeyId, request.GuestKey, cancellationToken);
        if (result == null)
            return NotFound(new { message = "Không tìm thấy hành trình hoặc guest key không hợp lệ / hành trình chưa bắt đầu." });

        return Ok(result);
    }

    [HttpPost("{journeyId:guid}/waypoints/{waypointId:guid}/checkin-guest")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(WaypointCheckInResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckInGuest(
        Guid journeyId,
        Guid waypointId,
        [FromBody] GuestWaypointCheckInRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || !ModelState.IsValid)
            return BadRequest(ModelState);

        var inner = new WaypointCheckInRequest
        {
            FeedbackText = request.FeedbackText,
            PhotoUrls = request.PhotoUrls
        };

        var result = await _journeyProgressService.CheckInGuestAsync(
            journeyId, waypointId, request.GuestKey, inner, cancellationToken);
        if (result == null)
            return NotFound(new { message = "Không tìm thấy waypoint hoặc guest key / hành trình chưa bắt đầu." });

        return Ok(result);
    }

    [HttpPost("{journeyId:guid}/waypoints/{waypointId:guid}/checkout-guest")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(WaypointCheckOutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckOutGuest(
        Guid journeyId,
        Guid waypointId,
        [FromBody] GuestWaypointCheckOutRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || !ModelState.IsValid)
            return BadRequest(ModelState);

        var inner = new WaypointCheckOutRequest { RatingValue = request.RatingValue };

        var result = await _journeyProgressService.CheckOutGuestAsync(
            journeyId, waypointId, request.GuestKey, inner, cancellationToken);
        if (result == null)
            return BadRequest(new { message = "Không thể check-out (rating 1–5 nếu gửi, hoặc waypoint không hợp lệ)." });

        return Ok(result);
    }

    /// <summary>Skip waypoint với tư cách khách (chỉ cập nhật tiến độ thành viên; không trả polyline).</summary>
    [HttpPost("{journeyId:guid}/waypoints/{waypointId:guid}/skip-guest")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(WaypointSkipResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SkipWaypointGuest(
        Guid journeyId,
        Guid waypointId,
        [FromBody] GuestKeyBodyRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || !ModelState.IsValid)
            return BadRequest(ModelState);

        var skipped = await _journeyProgressService.SkipWaypointGuestAsync(
            journeyId, waypointId, request.GuestKey, cancellationToken);
        if (skipped == null)
            return NotFound(new { message = "Không tìm thấy waypoint hoặc guest key / hành trình chưa bắt đầu." });

        return Ok(skipped);
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

    #region Live Location (REST pipeline: validate → Redis → SignalR broadcast)

    /// <summary>
    /// GPS realtime: app gửi vị trí hiện tại (1–3 giây / lần). Server lưu Redis (TTL 5 phút) rồi broadcast <c>MemberLocationUpdated</c> qua SignalR.
    /// JWT hoặc <c>guestKey</c>.
    /// </summary>
    [HttpPost("{journeyId:guid}/live-location")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLiveLocation(
        Guid journeyId,
        [FromBody] UpdateLiveLocationRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || !ModelState.IsValid)
            return BadRequest(ModelState);

        var (member, errorResult) = await ResolveLiveMemberAsync(journeyId, request.GuestKey, cancellationToken);
        if (errorResult != null) return errorResult;

        if (!_rateLimiter.TryAllow(member!.Id))
            return NoContent();

        var notification = new JourneyMemberLocationNotification
        {
            JourneyId = journeyId,
            MemberId = member.Id,
            TravelerId = member.TravelerId,
            GuestKey = member.GuestKey,
            DisplayName = member.DisplayName,
            Role = member.Role,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            AccuracyMeters = request.AccuracyMeters,
            HeadingDegrees = request.HeadingDegrees,
            AtUtc = DateTime.UtcNow
        };

        await _locationCache.SetAsync(notification, cancellationToken);
        await _liveNotifier.NotifyMemberLocationAsync(notification, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Snapshot vị trí mới nhất tất cả member đang active — gọi 1 lần khi mở map để khởi tạo markers.
    /// JWT hoặc <c>guestKey</c>.
    /// </summary>
    [HttpGet("{journeyId:guid}/live-locations")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<JourneyMemberLocationNotification>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLiveLocations(
        Guid journeyId,
        CancellationToken cancellationToken,
        [FromQuery] Guid? guestKey = null)
    {
        var (member, errorResult) = await ResolveLiveMemberAsync(journeyId, guestKey, cancellationToken);
        if (errorResult != null) return errorResult;

        var activeMembers = await _memberRepo.GetActiveMembersAsync(journeyId, cancellationToken);
        var memberIds = activeMembers.Select(m => m.Id);
        var locations = await _locationCache.GetAllForJourneyAsync(journeyId, memberIds, cancellationToken);

        return Ok(locations);
    }

    private async Task<(JSEA_Application.Models.JourneyMember? Member, IActionResult? ErrorResult)> ResolveLiveMemberAsync(
        Guid journeyId,
        Guid? guestKey,
        CancellationToken cancellationToken)
    {
        Guid? travelerId = null;
        if (User?.Identity?.IsAuthenticated == true &&
            Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var tid))
            travelerId = tid;

        if (!travelerId.HasValue && !guestKey.HasValue)
            return (null, Unauthorized(new { message = "Cần đăng nhập hoặc guestKey." }));

        if (travelerId.HasValue)
            guestKey = null;

        try
        {
            if (travelerId.HasValue)
                await _journeyService.VerifyTravelerCanNavigateStartedJourneyAsync(journeyId, travelerId.Value, cancellationToken);
            else
                await _journeyService.VerifyGuestCanNavigateStartedJourneyAsync(journeyId, guestKey!.Value, cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return (null, NotFound(new { message = "Không tìm thấy hành trình." }));
        }
        catch (UnauthorizedAccessException)
        {
            return (null, NotFound(new { message = "Không tìm thấy hành trình." }));
        }
        catch (InvalidOperationException)
        {
            return (null, BadRequest(new { message = "Hành trình chưa bắt đầu." }));
        }

        JSEA_Application.Models.JourneyMember? member = travelerId.HasValue
            ? await _memberRepo.GetActiveByTravelerAsync(journeyId, travelerId.Value, cancellationToken)
            : await _memberRepo.GetActiveByGuestKeyAsync(journeyId, guestKey!.Value, cancellationToken);

        if (member == null)
            return (null, NotFound(new { message = "Không tìm thấy thành viên." }));

        return (member, null);
    }

    #endregion
}
