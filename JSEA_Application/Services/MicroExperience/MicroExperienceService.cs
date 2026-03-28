using JSEA_Application.DTOs.Request.MicroExperience;
using JSEA_Application.DTOs.Respone.MicroExperience;
using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using JSEA_Application.Services.Journey;
using System.Text.RegularExpressions;
using ExperienceEntity = JSEA_Application.Models.Experience;

namespace JSEA_Application.Services.MicroExperience;

public class MicroExperienceService : IMicroExperienceService
{
    private readonly IMicroExperienceRepository _repository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IGoongMapsService _goongMapsService;
    private readonly EmbeddingGeneratorService _embeddingGenerator;

    public MicroExperienceService(
        IMicroExperienceRepository repository,
        ICategoryRepository categoryRepository,
        IGoongMapsService goongMapsService,
        EmbeddingGeneratorService embeddingGenerator)
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
        _goongMapsService = goongMapsService;
        _embeddingGenerator = embeddingGenerator;
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
            Status = x.Status,
            PreferredTimes = x.PreferredTimes,
            Latitude = x.Location?.Y,
            Longitude = x.Location?.X
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

        var fullAddress = BuildFullAddress(request.Address, request.City, request.Country ?? "Vietnam");
        var location = await _goongMapsService.GeocodeAddressToPointAsync(fullAddress, cancellationToken);
        if (location == null)
            return null;

        var accessibleBy = request.AccessibleBy ?? new List<string>();
        if (accessibleBy.Count == 0)
            accessibleBy = new List<string> { "walking" };

        var entity = new ExperienceEntity
        {
            Name = request.Name,
            Slug = slug,
            CategoryId = request.CategoryId,
            Address = request.Address,
            City = request.City,
            Country = request.Country ?? "Vietnam",
            Location = location,
            AccessibleBy = accessibleBy,
            PreferredTimes = request.PreferredTimes,
            WeatherSuitability = request.WeatherSuitability,
            Seasonality = request.Seasonality,
            AmenityTags = request.AmenityTags,
            Tags = request.Tags,
            Status = "active"
        };

        var saved = await _repository.SaveAsync(entity, cancellationToken);
        await ApplyExperienceDetailAsync(saved.Id, request.RichDescription, request.OpeningHours, request.PriceRange, request.CrowdLevel, cancellationToken);
        saved = await _repository.GetByIdAsync(saved.Id, cancellationToken)!;
        await _embeddingGenerator.RegenerateEmbeddingForExperienceAsync(saved!.Id, cancellationToken);
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
        entity.Status = request.Status ?? entity.Status;
        entity.PreferredTimes = request.PreferredTimes ?? entity.PreferredTimes;
        entity.WeatherSuitability = request.WeatherSuitability ?? entity.WeatherSuitability;
        entity.Seasonality = request.Seasonality ?? entity.Seasonality;
        if (request.AccessibleBy != null)
            entity.AccessibleBy = request.AccessibleBy;
        if (request.AmenityTags != null)
            entity.AmenityTags = request.AmenityTags;
        if (request.Tags != null)
            entity.Tags = request.Tags;

        var fullAddress = BuildFullAddress(request.Address, entity.City, entity.Country ?? "Vietnam");
        var location = await _goongMapsService.GeocodeAddressToPointAsync(fullAddress, cancellationToken);
        if (location == null)
            return null;
        entity.Location = location;

        var updated = await _repository.SaveAsync(entity, cancellationToken);
        await MergeExperienceDetailOnUpdateAsync(updated, request, cancellationToken);
        updated = await _repository.GetByIdAsync(updated.Id, cancellationToken)!;
        await _embeddingGenerator.RegenerateEmbeddingForExperienceAsync(updated!.Id, cancellationToken);
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

    private static bool HasAnyDetailInput(string? rich, string? hours, string? price, string? crowd) =>
        !string.IsNullOrWhiteSpace(rich) || !string.IsNullOrWhiteSpace(hours) ||
        !string.IsNullOrWhiteSpace(price) || !string.IsNullOrWhiteSpace(crowd);

    private async Task ApplyExperienceDetailAsync(
        Guid experienceId,
        string? richDescription,
        string? openingHours,
        string? priceRange,
        string? crowdLevel,
        CancellationToken cancellationToken)
    {
        if (!HasAnyDetailInput(richDescription, openingHours, priceRange, crowdLevel))
            return;

        var crowd = string.IsNullOrWhiteSpace(crowdLevel) ? "normal" : crowdLevel.Trim().ToLowerInvariant();
        await _repository.UpsertExperienceDetailAsync(new ExperienceDetail
        {
            ExperienceId = experienceId,
            RichDescription = string.IsNullOrWhiteSpace(richDescription) ? null : richDescription.Trim(),
            OpeningHours = string.IsNullOrWhiteSpace(openingHours) ? null : openingHours.Trim(),
            PriceRange = string.IsNullOrWhiteSpace(priceRange) ? null : priceRange.Trim(),
            CrowdLevel = crowd
        }, cancellationToken);
    }

    /// <summary>Cập nhật detail: chỉ trường nào có trên request thì ghi đè, còn lại giữ giá trị cũ.</summary>
    private async Task MergeExperienceDetailOnUpdateAsync(
        ExperienceEntity entity,
        UpdateMicroExperienceRequest request,
        CancellationToken cancellationToken)
    {
        if (request.RichDescription == null && request.OpeningHours == null && request.PriceRange == null &&
            request.CrowdLevel == null)
            return;

        var rich = request.RichDescription ?? entity.ExperienceDetail?.RichDescription;
        var hours = request.OpeningHours ?? entity.ExperienceDetail?.OpeningHours;
        var price = request.PriceRange ?? entity.ExperienceDetail?.PriceRange;
        var crowd = request.CrowdLevel ?? entity.ExperienceDetail?.CrowdLevel ?? "normal";
        crowd = string.IsNullOrWhiteSpace(crowd) ? "normal" : crowd.Trim().ToLowerInvariant();

        await _repository.UpsertExperienceDetailAsync(new ExperienceDetail
        {
            ExperienceId = entity.Id,
            RichDescription = string.IsNullOrWhiteSpace(rich) ? null : rich.Trim(),
            OpeningHours = string.IsNullOrWhiteSpace(hours) ? null : hours.Trim(),
            PriceRange = string.IsNullOrWhiteSpace(price) ? null : price.Trim(),
            CrowdLevel = crowd
        }, cancellationToken);
    }

    private static MicroExperienceDetailResponse MapToDetailResponse(ExperienceEntity entity)
    {
        return new MicroExperienceDetailResponse
        {
            Id = entity.Id,
            CategoryId = entity.CategoryId,
            Name = entity.Name,
            CategoryName = entity.Category?.Name,
            RichDescription = entity.ExperienceDetail?.RichDescription,
            AvgRating = entity.ExperienceMetric?.AvgRating ?? 0,
            QualityScore = entity.ExperienceMetric?.QualityScore ?? 0,
            Status = entity.Status,
            Address = entity.Address,
            City = entity.City,
            Country = entity.Country,
            AccessibleBy = entity.AccessibleBy,
            PreferredTimes = entity.PreferredTimes,
            WeatherSuitability = entity.WeatherSuitability,
            Seasonality = entity.Seasonality,
            AmenityTags = entity.AmenityTags,
            Tags = entity.Tags,
            OpeningHours = entity.ExperienceDetail?.OpeningHours,
            PriceRange = entity.ExperienceDetail?.PriceRange,
            CrowdLevel = entity.ExperienceDetail?.CrowdLevel,
            Latitude = entity.Location?.Y,
            Longitude = entity.Location?.X
        };
    }

    private static string Slugify(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "experience";
        var slug = name.Trim().ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "experience" : slug;
    }

    private static string BuildFullAddress(string? address, string? city, string? country)
    {
        var parts = new[] { address?.Trim(), city?.Trim(), country?.Trim() }
            .Where(s => !string.IsNullOrWhiteSpace(s));
        var full = string.Join(", ", parts);
        return string.IsNullOrWhiteSpace(full) ? "Vietnam" : full;
    }
}
