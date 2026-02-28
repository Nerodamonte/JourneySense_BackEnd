using JSEA_Application.DTOs.Request.MicroExperience;
using JSEA_Application.DTOs.Respone.MicroExperience;
using JSEA_Application.Enums;
using JSEA_Application.Interfaces;
using System.Text.RegularExpressions;
using MicroExperienceEntity = JSEA_Application.Models.MicroExperience;

namespace JSEA_Application.Services.MicroExperience;

public class MicroExperienceService : IMicroExperienceService
{
    private readonly IMicroExperienceRepository _repository;
    private readonly ICategoryRepository _categoryRepository;

    public MicroExperienceService(IMicroExperienceRepository repository, ICategoryRepository categoryRepository)
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
    }

    public async Task<List<MicroExperienceListItemResponse>> GetListAsync(MicroExperienceFilter filter, CancellationToken cancellationToken = default)
    {
        var filterDto = filter ?? new MicroExperienceFilter();
        var list = await _repository.FindAllAsync(filterDto, cancellationToken);

        return list.Select(x => new MicroExperienceListItemResponse
        {
            Id = x.Id,
            Name = x.Name,
            City = x.City,
            Status = x.Status
        }).ToList();
    }

    public async Task<MicroExperienceDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity == null ? null : MapToDetailResponse(entity);
    }

    public async Task<MicroExperienceDetailResponse?> CreateAsync(CreateMicroExperienceRequest request, CancellationToken cancellationToken = default)
    {
        if (!await _categoryRepository.ExistsAsync(request.CategoryId, cancellationToken))
            return null;

        var slug = Slugify(request.Name);
        if (await _repository.ExistsBySlugAsync(slug, cancellationToken))
            return null;

        var entity = new MicroExperienceEntity
        {
            Name = request.Name,
            Slug = slug,
            CategoryId = request.CategoryId,
            Address = request.Address,
            City = request.City,
            Country = request.Country ?? "Vietnam",
            Status = ExperienceStatus.ActiveUnverified
        };

        var saved = await _repository.SaveAsync(entity, cancellationToken);
        saved = await _repository.GetByIdAsync(saved.Id, cancellationToken)!;
        return MapToDetailResponse(saved!);
    }

    public async Task<MicroExperienceDetailResponse?> UpdateAsync(Guid id, UpdateMicroExperienceRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return null;

        if (!await _categoryRepository.ExistsAsync(request.CategoryId, cancellationToken))
            return null;

        var newSlug = Slugify(request.Name);
        if (newSlug != entity.Slug && await _repository.ExistsBySlugAsync(newSlug, cancellationToken))
            return null;

        entity.Name = request.Name;
        entity.Slug = newSlug;
        entity.CategoryId = request.CategoryId;
        entity.Address = request.Address;
        entity.Status = request.Status;

        var updated = await _repository.SaveAsync(entity, cancellationToken);
        updated = await _repository.GetByIdAsync(updated.Id, cancellationToken)!;
        return MapToDetailResponse(updated!);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return false;

        await _repository.DeleteAsync(id, cancellationToken);
        return true;
    }

    private static MicroExperienceDetailResponse MapToDetailResponse(MicroExperienceEntity entity)
    {
        return new MicroExperienceDetailResponse
        {
            Id = entity.Id,
            Name = entity.Name,
            CategoryName = entity.Category?.Name,
            Description = entity.ExperienceDetail?.Description,
            AvgRating = entity.ExperienceMetric?.AvgRating ?? 0,
            Status = entity.Status,
            Address = entity.Address,
            City = entity.City,
            Country = entity.Country
        };
    }

    private static string Slugify(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "micro-experience";
        var slug = name.Trim().ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "micro-experience" : slug;
    }
}
