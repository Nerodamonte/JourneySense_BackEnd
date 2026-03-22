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
