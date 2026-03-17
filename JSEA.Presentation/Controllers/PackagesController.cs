using JSEA_Application.DTOs.Request.Package;
using JSEA_Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JSEA_Presentation.Controllers;

[ApiController]
[Route("api/packages")]
public class PackagesController : ControllerBase
{
    private readonly IPackageService _packageService;

    public PackagesController(IPackageService packageService)
    {
        _packageService = packageService;
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        var list = await _packageService.GetListAsync(isActive, cancellationToken);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await _packageService.GetByIdAsync(id, cancellationToken);
        if (item == null)
            return NotFound(new { message = "Không tìm thấy gói dịch vụ." });
        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePackageDto dto, CancellationToken cancellationToken)
    {
        if (dto == null || !ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var created = await _packageService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePackageDto dto, CancellationToken cancellationToken)
    {
        if (dto == null || !ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var updated = await _packageService.UpdateAsync(id, dto, cancellationToken);
            if (updated == null)
                return NotFound(new { message = "Không tìm thấy gói dịch vụ." });
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _packageService.SoftDeleteAsync(id, cancellationToken);
        if (deleted == null)
            return NotFound(new { message = "Không tìm thấy gói dịch vụ." });
        return Ok(deleted);
    }
}

