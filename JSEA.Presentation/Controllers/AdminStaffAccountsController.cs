using System.Security.Claims;
using JSEA_Application.Constants;
using JSEA_Application.DTOs.Portal;
using JSEA_Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JSEA_Presentation.Controllers;

[ApiController]
[Route("api/admin/staff-accounts")]
[Authorize(Roles = AppRoles.Admin)]
public class AdminStaffAccountsController : ControllerBase
{
    private readonly IAdminUserService _adminUsers;

    public AdminStaffAccountsController(IAdminUserService adminUsers)
    {
        _adminUsers = adminUsers;
    }

    /// <summary>4) Tạo tài khoản staff (email + mật khẩu).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AdminUserDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateStaffAccountRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || !ModelState.IsValid)
            return BadRequest(ModelState);

        var actorId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (user, error) = await _adminUsers.CreateStaffAccountAsync(
            actorId,
            request,
            HttpContext.Connection.RemoteIpAddress,
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        if (user == null)
            return BadRequest(new { message = error });

        return Created($"/api/admin/users/{user.Id}", user);
    }
}
