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
        const string sql = @"
SELECT e.id AS ""Id"", e.name AS ""Name"", c.name AS ""CategoryName"", e.address AS ""Address"", e.city AS ""City"", e.country AS ""Country"",
  e.preferred_times AS ""PreferredTimes"", e.status AS ""Status"",
  ST_Y(e.location::geometry) AS ""Latitude"", ST_X(e.location::geometry) AS ""Longitude"",
  ROUND(ST_Distance(j.route_path::geography, e.location::geography))::int AS ""DetourDistanceMeters"",
  COALESCE(ed.estimated_duration_minutes, COALESCE(j.preferred_stop_duration_minutes, 15))::int AS ""EstimatedStopMinutes""
FROM experiences e
LEFT JOIN categories c ON c.id = e.category_id
LEFT JOIN experience_details ed ON ed.experience_id = e.id
CROSS JOIN journeys j
WHERE j.id = @p0
  AND e.location IS NOT NULL
  AND j.route_path IS NOT NULL
  AND ST_DWithin(j.route_path::geography, e.location::geography, COALESCE(j.max_detour_distance_meters, 2000))
  AND e.status = 'active'
  AND (j.current_mood_factor_id IS NULL OR EXISTS (SELECT 1 FROM experience_tags et WHERE et.experience_id = e.id AND et.factor_id = j.current_mood_factor_id))
  AND (@p2 IS NULL OR (e.weather_suitability IS NOT NULL AND e.weather_suitability @> ARRAY[@p2]::varchar[]))
  AND (@p3 IS NULL OR (e.preferred_times IS NOT NULL AND e.preferred_times @> ARRAY[@p3]::varchar[]))
ORDER BY ST_Distance(j.route_path::geography, e.location::geography)
LIMIT @p1";

        var rows = await _context.Set<RouteSuggestionSqlRow>()
            .FromSqlRaw(sql, journeyId, limit, weatherStr ?? (object)DBNull.Value, timeOfDayStr ?? (object)DBNull.Value)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return rows.Select(r => new RouteMicroExperienceSuggestionResponse
        {
            Id = r.Id,
            Name = r.Name,
            CategoryName = r.CategoryName,
            Address = r.Address,
            City = r.City,
            Country = r.Country,
            PreferredTimes = r.PreferredTimes,
            Status = r.Status,
            Latitude = r.Latitude,
            Longitude = r.Longitude,
            DetourDistanceMeters = r.DetourDistanceMeters,
            EstimatedStopMinutes = r.EstimatedStopMinutes
        }).ToList();
    }
}
