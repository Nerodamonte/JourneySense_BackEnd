using JSEA_Application.DTOs.Request.Reward;
using JSEA_Application.DTOs.Respone.Reward;
using JSEA_Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JSEA_Presentation.Controllers;

[ApiController]
[Route("api/reward-points")]
[Authorize]
public class RewardPointsController : ControllerBase
{
    private readonly IRewardService _rewardService;

    public RewardPointsController(IRewardService rewardService)
    {
        _rewardService = rewardService;
    }

    /// <summary>
    /// Lấy điểm (RewardPoints) theo userId.
    /// </summary>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(RewardPointsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByUserId(Guid userId, CancellationToken cancellationToken)
    {
        var points = await _rewardService.GetRewardPointsAsync(userId, cancellationToken);
        return Ok(new RewardPointsResponse { UserId = userId, RewardPoints = points });
    }

    /// <summary>
    /// Cộng điểm cho user.
    /// </summary>
    [HttpPost("add")]
    [ProducesResponseType(typeof(RewardPointsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Add([FromBody] RewardPointsAdjustRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await _rewardService.AddRewardPointsAsync(
            request.UserId,
            request.Points,
            request.Reason,
            cancellationToken,
            request.AchievementId,
            request.RefId,
            request.RefType);

        var points = await _rewardService.GetRewardPointsAsync(request.UserId, cancellationToken);
        return Ok(new RewardPointsResponse { UserId = request.UserId, RewardPoints = points });
    }

    /// <summary>
    /// Trừ điểm của user.
    /// </summary>
    [HttpPost("subtract")]
    [ProducesResponseType(typeof(RewardPointsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Subtract([FromBody] RewardPointsAdjustRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await _rewardService.SubtractRewardPointsAsync(
                request.UserId,
                request.Points,
                request.Reason,
                cancellationToken,
                request.RefId,
                request.RefType);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        var points = await _rewardService.GetRewardPointsAsync(request.UserId, cancellationToken);
        return Ok(new RewardPointsResponse { UserId = request.UserId, RewardPoints = points });
    }
}
