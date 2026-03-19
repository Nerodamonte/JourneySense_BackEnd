using JSEA_Application.DTOs.Request.Category;
using JSEA_Application.DTOs.Respone.Category;

namespace JSEA_Application.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryResponseDto>> GetActiveListAsync(CancellationToken cancellationToken = default);
    Task<CategoryResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CategoryResponseDto> CreateAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default);
    Task<CategoryResponseDto?> UpdateAsync(Guid id, UpdateCategoryDto dto, CancellationToken cancellationToken = default);
    Task<CategoryResponseDto?> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

