using JSEA_Application.DTOs.Request.Package;
using JSEA_Application.DTOs.Respone.Package;

namespace JSEA_Application.Interfaces;

public interface IPackageService
{
    Task<List<PackageResponseDto>> GetListAsync(bool? isActive, CancellationToken cancellationToken = default);
    Task<PackageResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PackageResponseDto> CreateAsync(CreatePackageDto dto, CancellationToken cancellationToken = default);
    Task<PackageResponseDto?> UpdateAsync(Guid id, UpdatePackageDto dto, CancellationToken cancellationToken = default);
    Task<PackageResponseDto?> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

