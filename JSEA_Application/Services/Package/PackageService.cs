using JSEA_Application.DTOs.Request.Package;
using JSEA_Application.DTOs.Respone.Package;
using JSEA_Application.Interfaces;
using JSEA_Application.Models;

namespace JSEA_Application.Services.Package;

public class PackageService : IPackageService
{
    private readonly IPackageRepository _packageRepository;

    public PackageService(IPackageRepository packageRepository)
    {
        _packageRepository = packageRepository;
    }

    public async Task<List<PackageResponseDto>> GetListAsync(bool? isActive, CancellationToken cancellationToken = default)
    {
        var list = await _packageRepository.GetListAsync(isActive, cancellationToken);
        return list.Select(MapToResponse).ToList();
    }

    public async Task<PackageResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _packageRepository.GetByIdAsync(id, cancellationToken);
        return entity == null ? null : MapToResponse(entity);
    }

    public async Task<PackageResponseDto> CreateAsync(CreatePackageDto dto, CancellationToken cancellationToken = default)
    {
        Validate(dto.Price, dto.SalePrice);

        var entity = new Models.Package
        {
            Title = dto.Title.Trim(),
            Price = dto.Price,
            SalePrice = dto.SalePrice,
            Type = dto.Type.Trim().ToLowerInvariant(),
            DistanceLimitKm = dto.DistanceLimitKm,
            DurationInDays = dto.DurationInDays,
            Benefit = dto.Benefit,
            IsPopular = dto.IsPopular,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        var saved = await _packageRepository.CreateAsync(entity, cancellationToken);
        return MapToResponse(saved);
    }

    public async Task<PackageResponseDto?> UpdateAsync(Guid id, UpdatePackageDto dto, CancellationToken cancellationToken = default)
    {
        Validate(dto.Price, dto.SalePrice);

        var entity = await _packageRepository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return null;

        entity.Title = dto.Title.Trim();
        entity.Price = dto.Price;
        entity.SalePrice = dto.SalePrice;
        entity.Type = dto.Type.Trim().ToLowerInvariant();
        entity.DistanceLimitKm = dto.DistanceLimitKm;
        entity.DurationInDays = dto.DurationInDays;
        entity.Benefit = dto.Benefit;
        entity.IsPopular = dto.IsPopular;
        entity.IsActive = dto.IsActive;

        await _packageRepository.UpdateAsync(entity, cancellationToken);
        return MapToResponse(entity);
    }

    public async Task<PackageResponseDto?> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _packageRepository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return null;

        entity.IsActive = false;
        await _packageRepository.UpdateAsync(entity, cancellationToken);
        return MapToResponse(entity);
    }

    private static void Validate(decimal price, decimal? salePrice)
    {
        if (price < 0)
            throw new ArgumentException("Price không hợp lệ.");
        if (salePrice.HasValue && salePrice.Value < 0)
            throw new ArgumentException("SalePrice không hợp lệ.");
        if (salePrice.HasValue && salePrice.Value > price)
            throw new ArgumentException("SalePrice không được lớn hơn Price.");
    }

    private static PackageResponseDto MapToResponse(Models.Package p)
    {
        return new PackageResponseDto
        {
            Id = p.Id,
            Title = p.Title,
            Price = p.Price,
            SalePrice = p.SalePrice,
            Type = p.Type,
            DistanceLimitKm = p.DistanceLimitKm,
            DurationInDays = p.DurationInDays,
            Benefit = p.Benefit,
            IsPopular = p.IsPopular,
            IsActive = p.IsActive ?? false,
            CreatedAt = p.CreatedAt
        };
    }

}

