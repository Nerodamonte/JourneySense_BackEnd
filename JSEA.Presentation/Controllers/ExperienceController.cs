using JSEA_Application.DTOs.Request.Experience;
using JSEA_Application.DTOs.Respone.Experience;
using JSEA_Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JSEA_Presentation.Controllers;

[ApiController]
[Route("api/experiences")]
public class ExperienceController : ControllerBase
{
    private readonly IRateFeedbackService _rateFeedbackService;

    public ExperienceController(IRateFeedbackService rateFeedbackService)
    {
        _rateFeedbackService = rateFeedbackService;
    }

    /// <summary>
    /// Đánh dấu đã ghé thăm + gửi đánh giá (1-5) và/hoặc feedback, ảnh. Cộng điểm thưởng. Yêu cầu đăng nhập. Mỗi (traveler, experience, journey) chỉ được gửi một lần.
    /// </summary>
    [HttpPost("visit")]
    [Authorize]
    [ProducesResponseType(typeof(VisitFeedbackResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitVisitFeedback(
        [FromBody] VisitFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || !ModelState.IsValid)
            return BadRequest(ModelState);

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var travelerId))
            return Unauthorized(new { message = "Vui lòng đăng nhập." });

        var result = await _rateFeedbackService.CreateVisitWithFeedbackAsync(request, travelerId, cancellationToken);
        if (result == null)
            return Conflict(new { message = "Bạn đã đánh dấu ghé thăm trải nghiệm này trong hành trình này rồi." });

        return Created($"/api/experiences/visit/{result.VisitId}", result);
    }
}
