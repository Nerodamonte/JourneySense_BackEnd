using JSEA_Application.Constants;
using JSEA_Application.DTOs.Portal;
using JSEA_Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JSEA_Presentation.Controllers;

[ApiController]
[Route("api/admin/analytics")]
[Authorize(Roles = AppRoles.Admin)]
public class AdminAnalyticsController : ControllerBase
{
    private readonly IAdminAnalyticsService _analytics;

    public AdminAnalyticsController(IAdminAnalyticsService analytics)
    {
        _analytics = analytics;
    }

    /// <summary>5) Tổng quan thống kê hệ thống.</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(AdminAnalyticsSummaryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken)
    {
        var s = await _analytics.GetSummaryAsync(cancellationToken);
        return Ok(s);
    }
}
