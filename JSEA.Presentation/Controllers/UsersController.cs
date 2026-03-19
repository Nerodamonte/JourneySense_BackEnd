using JSEA_Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JSEA_Presentation.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserPackageService _userPackageService;

    public UsersController(IUserPackageService userPackageService)
    {
        _userPackageService = userPackageService;
    }

    [HttpGet("{userId:guid}/current-package")]
    public async Task<IActionResult> GetCurrentPackage(Guid userId, CancellationToken cancellationToken)
    {
        var current = await _userPackageService.GetCurrentPackageByUserIdAsync(userId, cancellationToken);
        if (current == null)
            return NotFound(new { message = "Người dùng hiện không có gói nào đang kích hoạt." });
        return Ok(current);
    }
}