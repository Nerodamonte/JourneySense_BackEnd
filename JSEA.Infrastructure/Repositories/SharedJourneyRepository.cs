using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Infrastructure.Repositories;

public class SharedJourneyRepository : ISharedJourneyRepository
{
    private readonly AppDbContext _context;

    public SharedJourneyRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SharedJourney?> GetActiveByJourneyAndUserAsync(
        Guid journeyId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.SharedJourneys
            .FirstOrDefaultAsync(
                s => s.JourneyId == journeyId && s.UserId == userId && s.IsActive,
                cancellationToken);
    }

    public async Task<SharedJourney?> GetByShareCodeWithJourneyAsync(
        string shareCode,
        CancellationToken cancellationToken = default)
    {
        return await _context.SharedJourneys
            .Include(s => s.Journey)
                .ThenInclude(j => j.JourneyWaypoints)
            .FirstOrDefaultAsync(s => s.ShareCode == shareCode && s.IsActive, cancellationToken);
    }

    public async Task<List<SharedJourney>> GetPublicCompletedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 50) pageSize = 50;

        return await _context.SharedJourneys
            .AsNoTracking()
            .Where(s => s.IsActive
                        && s.Journey != null
                        && s.Journey.Status != null
                        && EF.Functions.ILike(s.Journey.Status, "completed"))
            .Include(s => s.Journey)
                .ThenInclude(j => j.JourneyWaypoints)
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<SharedJourney?> GetPublicDetailAsync(string shareCode, CancellationToken cancellationToken = default)
    {
        return await _context.SharedJourneys
            .Include(s => s.Journey)
                .ThenInclude(j => j.JourneyWaypoints)
                    .ThenInclude(w => w.Experience)
                        .ThenInclude(e => e.ExperienceDetail)
            .Include(s => s.Journey)
                .ThenInclude(j => j.JourneyWaypoints)
                    .ThenInclude(w => w.Experience)
                        .ThenInclude(e => e.ExperiencePhotos)
            .FirstOrDefaultAsync(
                s => s.ShareCode == shareCode
                     && s.IsActive
                     && s.Journey != null
                     && s.Journey.Status != null
                     && EF.Functions.ILike(s.Journey.Status, "completed"),
                cancellationToken);
    }

    public async Task<bool> ShareCodeExistsAsync(string shareCode, CancellationToken cancellationToken = default)
    {
        return await _context.SharedJourneys.AnyAsync(s => s.ShareCode == shareCode, cancellationToken);
    }

    public async Task<SharedJourney> AddAsync(SharedJourney entity, CancellationToken cancellationToken = default)
    {
        if (entity.Id == Guid.Empty)
            entity.Id = Guid.NewGuid();
        entity.CreatedAt ??= DateTime.UtcNow;
        _context.SharedJourneys.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(SharedJourney entity, CancellationToken cancellationToken = default)
    {
        _context.SharedJourneys.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
