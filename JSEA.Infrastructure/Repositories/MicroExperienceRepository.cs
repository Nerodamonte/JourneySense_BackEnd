using JSEA_Application.DTOs.Request.MicroExperience;
using JSEA_Application.DTOs.Respone.Journey;
using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using JSEA_Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using System.Text.Json;

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

        if (!string.IsNullOrWhiteSpace(filter.TimeOfDay))
        {
            query = query.Where(x => x.PreferredTimes != null && x.PreferredTimes.Contains(filter.TimeOfDay));
        }

        return await query
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Experience?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Experiences
            .Include(x => x.Category)
            .Include(x => x.ExperienceDetail)
            .Include(x => x.ExperienceMetric)
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
    public async Task<List<Experience>> FindCandidatesAsync(
       string vehicleType,
       string preferredCrowdLevel,
       LineString segmentPath,
       int maxDetourDistanceMeters,
       List<Guid> excludeIds,
       CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow.AddHours(7); // UTC+7
        var dayKey = now.DayOfWeek.ToString()[..3].ToLower(); // mon/tue/wed...

        var candidates = await _context.Experiences
            .AsNoTracking()
            .Include(e => e.Category)
            .Include(e => e.ExperienceDetail)
            .Include(e => e.ExperienceMetric)
            .Include(e => e.ExperiencePhotos)
            .Where(e => e.Status == "active")
            .Where(e => !excludeIds.Contains(e.Id))
            // Condition 1: vehicle_type
            .Where(e => e.AccessibleBy.Contains(vehicleType.ToLower()))
            // Condition 2: crowd_level (skip nếu preferredCrowdLevel = "all")
            .Where(e => preferredCrowdLevel == "all"
                || e.ExperienceDetail == null
                || e.ExperienceDetail.CrowdLevel == preferredCrowdLevel)
            // Condition 3: detour distance
            .Where(e => e.Location != null
                && e.Location.Distance(segmentPath) <= maxDetourDistanceMeters)
            .ToListAsync(cancellationToken);

        // Condition 7: opening_hours — filter in-memory vì jsonb khó query trong EF
        candidates = candidates.Where(e => IsOpenNow(e.ExperienceDetail?.OpeningHours, dayKey, now)).ToList();

        return candidates;
    }

    public async Task<Dictionary<Guid, decimal>> GetActiveEventBoostsAsync(
     List<Guid> experienceIds,
     CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var boosts = await _context.EventOccurrences
            .AsNoTracking()
            .Include(o => o.Event)
            .Where(o => o.Event != null
                && experienceIds.Contains(o.Event.ExperienceId)
                && o.OccurrenceStart <= now
                && o.OccurrenceEnd >= now)
            .GroupBy(o => o.Event!.ExperienceId)
            .Select(g => new
            {
                ExperienceId = g.Key,
                Boost = g.Max(o => o.Event!.ScoreBoostFactor ?? 1.0m)
            })
            .ToListAsync(cancellationToken);

        return boosts.ToDictionary(b => b.ExperienceId, b => b.Boost);
    }
    // =========================================================
    // PRIVATE: Helpers
    // =========================================================

    private static bool IsOpenNow(string? openingHoursJson, string dayKey, DateTime now)
    {
        if (string.IsNullOrEmpty(openingHoursJson)) return true; // NULL = không filter
        try
        {
            using var doc = JsonDocument.Parse(openingHoursJson);
            if (!doc.RootElement.TryGetProperty(dayKey, out var hoursEl)) return true;

            var hours = hoursEl.GetString();
            if (string.IsNullOrEmpty(hours)) return true;

            var parts = hours.Split('-');
            if (parts.Length != 2) return true;

            var open = TimeOnly.Parse(parts[0].Trim());
            var close = TimeOnly.Parse(parts[1].Trim());
            var current = TimeOnly.FromDateTime(now);

            return current >= open && current <= close;
        }
        catch
        {
            return true; // parse lỗi thì không filter
        }
    }

    public async Task<List<Experience>> GetActiveWithoutEmbeddingAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Experiences
            .AsNoTracking()
            .Include(e => e.Category)
            .Include(e => e.ExperienceDetail)
            .Where(e => e.Status == "active")
            .Where(e => !_context.ExperienceEmbeddings.Any(ee => ee.ExperienceId == e.Id))
            .ToListAsync(cancellationToken);
    }
}
