using System.Text.RegularExpressions;
using JSEA_Application.DTOs.Request.Category;
using JSEA_Application.DTOs.Respone.Category;
using JSEA_Application.Interfaces;
using JSEA_Application.Models;

namespace JSEA_Application.Services.Category;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<List<CategoryResponseDto>> GetActiveListAsync(CancellationToken cancellationToken = default)
    {
        var list = await _categoryRepository.GetActiveListAsync(cancellationToken);
        return list.Select(MapToResponse).ToList();
    }

    public async Task<CategoryResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        return entity == null ? null : MapToResponse(entity);
    }

    public async Task<CategoryResponseDto> CreateAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new Models.Category
        {
            Name = dto.Name.Trim(),
            Slug = string.IsNullOrWhiteSpace(dto.Slug) ? Slugify(dto.Name) : dto.Slug.Trim().ToLowerInvariant(),
            Description = dto.Description,
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive
        };

        var saved = await _categoryRepository.CreateAsync(entity, cancellationToken);
        return MapToResponse(saved);
    }

    public async Task<CategoryResponseDto?> UpdateAsync(Guid id, UpdateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return null;

        entity.Name = dto.Name.Trim();
        entity.Slug = string.IsNullOrWhiteSpace(dto.Slug) ? Slugify(dto.Name) : dto.Slug.Trim().ToLowerInvariant();
        entity.Description = dto.Description;
        entity.DisplayOrder = dto.DisplayOrder;
        entity.IsActive = dto.IsActive;

        await _categoryRepository.UpdateAsync(entity, cancellationToken);
        return MapToResponse(entity);
    }

    public async Task<CategoryResponseDto?> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return null;

        entity.IsActive = false;
        await _categoryRepository.UpdateAsync(entity, cancellationToken);
        return MapToResponse(entity);
    }

    private static CategoryResponseDto MapToResponse(Models.Category c)
    {
        return new CategoryResponseDto
        {
            Id = c.Id,
            Name = c.Name ?? "",
            Slug = c.Slug ?? "",
            Description = c.Description,
            DisplayOrder = c.DisplayOrder ?? 0,
            IsActive = c.IsActive ?? false
        };
    }

    private static string Slugify(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "category";
        var slug = name.Trim().ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "category" : slug;
    }
}