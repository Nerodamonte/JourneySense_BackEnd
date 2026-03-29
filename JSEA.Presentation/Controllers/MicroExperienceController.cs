using JSEA_Application.Constants;
using JSEA_Application.DTOs.Request.MicroExperience;
using JSEA_Application.DTOs.Respone.MicroExperience;
using JSEA_Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
            return BadRequest(new { message = "Danh mục không tồn tại, slug đã tồn tại, hoặc không có vị trí hợp lệ (gửi latitude+longitude hoặc địa chỉ geocode được trên Goong)." });

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
            return BadRequest(new { message = "Không tìm thấy trải nghiệm, danh mục không tồn tại, slug mới đã tồn tại, hoặc không có vị trí hợp lệ (latitude+longitude hoặc địa chỉ Goong)." });

        return Ok(result);
    }

    /// <summary>Staff: upload ảnh (multipart). Trả về metadata + URL tương đối (gắn domain API khi hiển thị).</summary>
    [HttpPost("{id:guid}/photos")]
    [Authorize(Roles = AppRoles.Staff)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ExperiencePhotoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadPhoto(
        Guid id,
        IFormFile file,
        [FromForm] string? caption,
        [FromForm] bool isCover = false,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Chọn file ảnh (file)." });

        Guid? actorId = null;
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(claim, out var uid))
            actorId = uid;

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _microExperienceService.UploadPhotoAsync(
                id,
                stream,
                file.ContentType ?? "application/octet-stream",
                file.FileName,
                caption,
                isCover,
                actorId,
                cancellationToken);

            if (result == null)
                return NotFound(new { message = "Không tìm thấy trải nghiệm." });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Staff: xóa một ảnh (và file nội bộ nếu lưu trong uploads).</summary>
    [HttpDelete("{id:guid}/photos/{photoId:guid}")]
    [Authorize(Roles = AppRoles.Staff)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePhoto(Guid id, Guid photoId, CancellationToken cancellationToken)
    {
        var ok = await _microExperienceService.DeletePhotoAsync(id, photoId, cancellationToken);
        if (!ok)
            return NotFound(new { message = "Không tìm thấy ảnh." });
        return NoContent();
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
