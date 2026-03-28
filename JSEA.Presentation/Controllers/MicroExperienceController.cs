using JSEA_Application.Constants;
using JSEA_Application.DTOs.Request.MicroExperience;
using JSEA_Application.DTOs.Respone.MicroExperience;
using JSEA_Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JSEA_Presentation.Controllers;

[ApiController]
[Route("api/micro-experiences")]
public class MicroExperienceController : ControllerBase
{
    private readonly IMicroExperienceService _microExperienceService;

    public MicroExperienceController(IMicroExperienceService microExperienceService)
    {
        _microExperienceService = microExperienceService;
    }


    /// <summary>Public read: mobile, admin (chỉ xem), staff. Không yêu cầu JWT.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MicroExperienceListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] string? keyword,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? status,
        [FromQuery] string? mood,
        [FromQuery] string? timeOfDay,
        CancellationToken cancellationToken)
    {
        var filter = new MicroExperienceFilter
        {
            Keyword = keyword,
            CategoryId = categoryId,
            Status = status,
            Mood = mood,
            TimeOfDay = timeOfDay
        };
        var list = await _microExperienceService.GetListAsync(filter, cancellationToken);
        return Ok(list);
    }


    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MicroExperienceDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var detail = await _microExperienceService.GetByIdAsync(id, cancellationToken);
        if (detail == null)
            return NotFound(new { message = "Không tìm thấy trải nghiệm." });
        return Ok(detail);
    }

   
    [HttpPost]
    [Authorize(Roles = AppRoles.Staff)]
    [ProducesResponseType(typeof(MicroExperienceDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateMicroExperienceRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || !ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _microExperienceService.CreateAsync(request, cancellationToken);
        if (result == null)
            return BadRequest(new { message = "Danh mục không tồn tại, slug đã tồn tại, hoặc địa chỉ không tìm thấy trên bản đồ (Goong). Kiểm tra categoryId, tên và địa chỉ (Address, City, Country)." });

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

   
    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.Staff)]
    [ProducesResponseType(typeof(MicroExperienceDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateMicroExperienceRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || !ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _microExperienceService.UpdateAsync(id, request, cancellationToken);
        if (result == null)
            return BadRequest(new { message = "Không tìm thấy trải nghiệm, danh mục không tồn tại, slug mới đã tồn tại, hoặc địa chỉ không tìm thấy trên bản đồ (Goong)." });

        return Ok(result);
    }

   
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.Staff)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _microExperienceService.DeleteAsync(id, cancellationToken);
        if (!deleted)
            return NotFound(new { message = "Không tìm thấy trải nghiệm." });
        return NoContent();
    }
}
