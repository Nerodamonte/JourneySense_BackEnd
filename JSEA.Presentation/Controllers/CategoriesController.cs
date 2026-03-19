using JSEA_Application.DTOs.Request.Category;
using JSEA_Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JSEA_Presentation.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveList(CancellationToken cancellationToken)
    {
        var list = await _categoryService.GetActiveListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await _categoryService.GetByIdAsync(id, cancellationToken);
        if (item == null)
            return NotFound(new { message = "Không tìm thấy danh mục." });
        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto, CancellationToken cancellationToken)
    {
        if (dto == null || !ModelState.IsValid)
            return BadRequest(ModelState);

        var created = await _categoryService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryDto dto, CancellationToken cancellationToken)
    {
        if (dto == null || !ModelState.IsValid)
            return BadRequest(ModelState);

        var updated = await _categoryService.UpdateAsync(id, dto, cancellationToken);
        if (updated == null)
            return NotFound(new { message = "Không tìm thấy danh mục." });
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _categoryService.SoftDeleteAsync(id, cancellationToken);
        if (deleted == null)
            return NotFound(new { message = "Không tìm thấy danh mục." });
        return Ok(deleted);
    }
}