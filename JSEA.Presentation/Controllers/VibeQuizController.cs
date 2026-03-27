using JSEA_Application.DTOs.Request.Quiz;
using JSEA_Application.DTOs.Respone.Quiz;
using JSEA_Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JSEA_Presentation.Controllers;

[ApiController]
[Route("api/vibes/quiz")]
public class VibeQuizController : ControllerBase
{
    private readonly IVibeQuizService _vibeQuizService;

    public VibeQuizController(IVibeQuizService vibeQuizService)
    {
        _vibeQuizService = vibeQuizService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(VibeQuizResponse), StatusCodes.Status200OK)]
    public IActionResult GetQuiz()
    {
        return Ok(_vibeQuizService.GetQuiz());
    }

    [HttpPost("submit")]
    [Authorize]
    [ProducesResponseType(typeof(SubmitVibeQuizResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Submit([FromBody] SubmitVibeQuizRequest request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Vui lòng đăng nhập." });

        try
        {
            var result = await _vibeQuizService.SubmitAsync(userId, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
