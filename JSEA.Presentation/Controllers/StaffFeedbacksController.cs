using System.Security.Claims;
using JSEA_Application.Constants;
using JSEA_Application.DTOs.Portal;
using JSEA_Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JSEA_Presentation.Controllers;

[ApiController]
[Route("api/staff/feedbacks")]
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Staff}")]
public class StaffFeedbacksController : ControllerBase
{
    private readonly IStaffFeedbackService _staffFeedback;

    public StaffFeedbacksController(IStaffFeedbackService staffFeedback)
    {
        _staffFeedback = staffFeedback;
    }

    /// <summary>7) Danh sách feedback (lọc moderationStatus, experienceId).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PortalPagedResult<StaffFeedbackListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? moderationStatus,
        [FromQuery] Guid? experienceId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var r = await _staffFeedback.ListAsync(moderationStatus, experienceId, page, pageSize, cancellationToken);
        return Ok(r);
    }

    ///<summary>8) Chi tiết một feedback.</summary>
    [HttpGet("{feedbackId:guid}")]
    [ProducesResponseType(typeof(StaffFeedbackDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid feedbackId, CancellationToken cancellationToken)
    {
        var f = await _staffFeedback.GetByIdAsync(feedbackId, cancellationToken);
        if (f == null)
            return NotFound(new { message = "Không tìm thấy feedback." });
        return Ok(f);
    }

    /// <summary>9) Duyệt (approve) hoặc từ chối (reject) feedback.</summary>
    [HttpPost("{feedbackId:guid}/moderate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Moderate(
        Guid feedbackId,
        [FromBody] ModerateFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || !ModelState.IsValid)
            return BadRequest(ModelState);

        var actorId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (ok, error) = await _staffFeedback.ModerateAsync(
            actorId,
            feedbackId,
            request,
            HttpContext.Connection.RemoteIpAddress,
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        if (!ok)
            return BadRequest(new { message = error });
        return NoContent();
    }
}
