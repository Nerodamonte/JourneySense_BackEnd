using JSEA_Application.DTOs.Request.MicroExperience;
using JSEA_Application.DTOs.Respone.Journey;
using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using JSEA_Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Infrastructure.Repositories;

public class MicroExperienceRepository : IMicroExperienceRepository
{
    private readonly AppDbContext _context;

    public MicroExperienceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Experience>> FindAllAsync(MicroExperienceFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Experiences.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim().ToLower();
            query = query.Where(x =>
                (x.Name != null && x.Name.ToLower().Contains(keyword)) ||
                (x.Address != null && x.Address.ToLower().Contains(keyword)) ||
                (x.City != null && x.City.ToLower().Contains(keyword)));
        }

        if (filter.CategoryId.HasValue)
            query = query.Where(x => x.CategoryId == filter.CategoryId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(x => x.Status == filter.Status);

        if (!string.IsNullOrWhiteSpace(filter.Mood))
        {
            query = query.Where(x => x.ExperienceTags.Any(et => et.Factor.Name == filter.Mood && et.Factor.Type == "mood"));
        }

        if (!string.IsNullOrWhiteSpace(filter.TimeOfDay))
        {
            query = query.Where(x => x.PreferredTimes != null && x.PreferredTimes.Contains(filter.TimeOfDay));
        }

        return await query
            .Include(x => x.ExperienceTags).ThenInclude(et => et.Factor)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Experience?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Experiences
            .Include(x => x.Category)
            .Include(x => x.ExperienceDetail)
            .Include(x => x.ExperienceMetric)
            .Include(x => x.ExperienceTags).ThenInclude(et => et.Factor)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.Experiences
            .AnyAsync(x => x.Slug == slug, cancellationToken);
    }

    public async Task<Experience> SaveAsync(Experience entity, CancellationToken cancellationToken = default)
    {
        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            _context.Experiences.Add(entity);
        }
        else
        {
            entity.UpdatedAt = DateTime.UtcNow;
            _context.Experiences.Update(entity);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Experiences.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.Experiences.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<List<RouteMicroExperienceSuggestionResponse>> FindSuggestionsAlongRouteAsync(Guid journeyId, int limit, string? weatherStr, string? timeOfDayStr, CancellationToken cancellationToken = default)
    {
        var journey = await _context.Journeys
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == journeyId, cancellationToken);

        if (journey == null || journey.RoutePath == null)
            return new List<RouteMicroExperienceSuggestionResponse>();

        var maxDetour = journey.MaxDetourDistanceMeters > 0 ? journey.MaxDetourDistanceMeters : 2000;
        var maxCount = limit is > 0 and <= 100 ? limit : 20;

        var query = _context.Experiences
            .AsNoTracking()
            .Include(e => e.Category)
            .Include(e => e.ExperienceDetail)
            .Include(e => e.ExperienceTags)
            .Where(e => e.Location != null && e.Status == "active")
            .Where(e => e.Location!.Distance(journey.RoutePath) <= maxDetour);

        if (journey.CurrentMoodFactorId.HasValue)
        {
            var moodId = journey.CurrentMoodFactorId.Value;
            query = query.Where(e => e.ExperienceTags.Any(et => et.FactorId == moodId));
        }

        if (!string.IsNullOrWhiteSpace(weatherStr))
        {
            query = query.Where(e =>
                e.WeatherSuitability != null &&
                e.WeatherSuitability.Contains(weatherStr));
        }

        if (!string.IsNullOrWhiteSpace(timeOfDayStr))
        {
            query = query.Where(e =>
                e.PreferredTimes != null &&
                e.PreferredTimes.Contains(timeOfDayStr));
        }

        var experiences = await query
            .OrderBy(e => e.Location!.Distance(journey.RoutePath))
            .Take(maxCount)
            .ToListAsync(cancellationToken);

        return experiences.Select(e => new RouteMicroExperienceSuggestionResponse
        {
            Id = e.Id,
            Name = e.Name,
            CategoryName = e.Category?.Name,
            Address = e.Address,
            City = e.City,
            Country = e.Country,
            PreferredTimes = e.PreferredTimes,
            Status = e.Status,
            Latitude = e.Location.Y,
            Longitude = e.Location.X,
            DetourDistanceMeters = (int)Math.Round(e.Location.Distance(journey.RoutePath)),
            EstimatedStopMinutes = e.ExperienceDetail?.EstimatedDurationMinutes
                ?? journey.PreferredStopDurationMinutes
                ?? 15
        }).ToList();
    }

    public async Task<int> CountAlongRouteAsync(NetTopologySuite.Geometries.LineString? routePath, int maxDetourDistanceMeters, CancellationToken cancellationToken = default)
    {
        if (routePath == null)
            return 0;

        // Đếm experiences active gần tuyến (không phụ thuộc journeys, không dùng mood/time/weather để tránh quá nặng).
        var distance = maxDetourDistanceMeters > 0 ? maxDetourDistanceMeters : 2000;

        // Sử dụng LINQ + NetTopologySuite để EF/Npgsql tự dịch sang ST_Distance/ST_DWithin.
        // e.Location: geography(Point,4326)
        // routePath : geometry(LineString,4326) được truyền vào như tham số.
        return await _context.Experiences
            .AsNoTracking()
            .Where(e => e.Location != null && e.Status == "active")
            .Where(e => e.Location!.Distance(routePath) <= distance)
            .CountAsync(cancellationToken);
    }
}
