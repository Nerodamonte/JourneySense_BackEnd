using System.Security.Claims;
using JSEA_Application.Constants;
using JSEA_Application.DTOs.Portal;
using JSEA_Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JSEA_Presentation.Controllers;

[ApiController]
[Route("api/staff/reports")]
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Staff}")]
public class StaffUserReportsController : ControllerBase
{
    private readonly IStaffFeedbackService _staffFeedback;

    public StaffUserReportsController(IStaffFeedbackService staffFeedback)
    {
        _staffFeedback = staffFeedback;
    }

    /// <summary>10) Report user (spam/vi phạm) — suspend tài khoản traveler.</summary>
    [HttpPost("users/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReportUser(
        Guid userId,
        [FromBody] ReportPortalUserRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || !ModelState.IsValid)
            return BadRequest(ModelState);

        var actorId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (ok, error) = await _staffFeedback.ReportUserAsync(
            actorId,
            userId,
            request,
            HttpContext.Connection.RemoteIpAddress,
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        if (!ok)
            return BadRequest(new { message = error });
        return NoContent();
    }
}
