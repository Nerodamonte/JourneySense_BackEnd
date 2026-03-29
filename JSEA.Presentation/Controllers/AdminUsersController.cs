using System.Security.Claims;
using JSEA_Application.Constants;
using JSEA_Application.DTOs.Portal;
using JSEA_Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JSEA_Presentation.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = AppRoles.Admin)]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _adminUsers;

    public AdminUsersController(IAdminUserService adminUsers)
    {
        _adminUsers = adminUsers;
    }

    /// <summary>1) Danh sách tài khoản (filter role, status, search email/phone).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PortalPagedResult<AdminUserListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? role,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _adminUsers.ListUsersAsync(role, status, search, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>2) Chi tiết một user.</summary>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(AdminUserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid userId, CancellationToken cancellationToken)
    {
        var u = await _adminUsers.GetUserByIdAsync(userId, cancellationToken);
        if (u == null)
            return NotFound(new { message = "Không tìm thấy user." });
        return Ok(u);
    }

    /// <summary>3) Đổi trạng thái (active | suspended).</summary>
    [HttpPatch("{userId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateStatus(
        Guid userId,
        [FromBody] UpdatePortalUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || !ModelState.IsValid)
            return BadRequest(ModelState);

        var actorId = GetRequiredUserId();
        if (actorId == Guid.Empty)
            return Unauthorized(new { message = "Token không hợp lệ." });

        var (ok, error) = await _adminUsers.UpdateUserStatusAsync(
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

    private Guid GetRequiredUserId()
    {
        var v = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(v) || !Guid.TryParse(v, out var id))
            return Guid.Empty;
        return id;
    }
}
