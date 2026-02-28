using JSEA_Application.DTOs.Request.Journey;
using JSEA_Application.DTOs.Respone.Journey;
using JSEA_Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JSEA_Presentation.Controllers;

[ApiController]
[Route("api/journeys")]
public class JourneyController : ControllerBase
{
    private readonly IJourneyService _journeyService;

    public JourneyController(IJourneyService journeyService)
    {
        _journeyService = journeyService;
    }

    /// <summary>
    /// Thiết lập hành trình: nhập điểm đi, điểm đến, loại xe, thời gian, độ lệch. Gọi Goong Maps để phân tích tuyến và lưu journey + waypoints.
    /// </summary>
    [HttpPost("setup")]
    [ProducesResponseType(typeof(JourneySetupResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> SetupJourney(
        [FromBody] JourneySetupRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || !ModelState.IsValid)
            return BadRequest(ModelState);

        Guid? travelerId = null;
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdClaim, out var uid))
            travelerId = uid;

        var result = await _journeyService.ValidateAndCreateJourneyAsync(request, travelerId, cancellationToken);
        if (result == null)
            return StatusCode(502, new { message = "Không thể phân tích tuyến. Kiểm tra địa chỉ hoặc API Goong Maps." });

        return Created($"/api/journeys/{result.JourneyId}", result);
    }
}
