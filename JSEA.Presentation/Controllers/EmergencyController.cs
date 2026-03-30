using JSEA_Application.DTOs.Request.Journey;
using JSEA_Application.DTOs.Respone.Journey;
using JSEA_Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JSEA_Presentation.Controllers;

/// <summary>Khẩn cấp một chạm: type + GPS → địa điểm gần nhất (Goong, không DB).</summary>
[ApiController]
[Route("api/emergency")]
public class EmergencyController : ControllerBase
{
    private readonly IEmergencyNearbyService _emergencyNearbyService;

    public EmergencyController(IEmergencyNearbyService emergencyNearbyService)
    {
        _emergencyNearbyService = emergencyNearbyService;
    }

    /// <summary>
    /// FE: type = repair_shop | hospital | pharmacy | gas_station | restaurant | lodging | coffee (+ lat/lng). Mặc định 1 kết quả gần nhất.
    /// </summary>
    [HttpPost("nearby")]
    [Authorize]
    [ProducesResponseType(typeof(List<EmergencyNearbyItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Nearby([FromBody] EmergencyNearbyRequest request, CancellationToken cancellationToken)
    {
        if (request == null || !ModelState.IsValid)
            return BadRequest(ModelState);

        var (status, message, items) = await _emergencyNearbyService.GetNearbyAsync(request, cancellationToken);
        if (status == 200)
            return Ok(items);
        return BadRequest(new { message = message });
    }
}
