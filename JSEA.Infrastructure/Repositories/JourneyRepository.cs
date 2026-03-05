using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Infrastructure.Repositories;

public class JourneyRepository : IJourneyRepository
{
    private readonly AppDbContext _context;

    public JourneyRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Journey> SaveAsync(Journey journey, List<JourneyWaypoint> waypoints, CancellationToken cancellationToken = default)
    {
        if (journey.Id == Guid.Empty)
        {
            journey.Id = Guid.NewGuid();
            journey.CreatedAt = DateTime.UtcNow;
            _context.Journeys.Add(journey);
        }
        else
        {
            _context.Journeys.Update(journey);
        }

        foreach (var wp in waypoints.Where(w => w.ExperienceId != Guid.Empty))
        {
            if (wp.Id == Guid.Empty)
                wp.Id = Guid.NewGuid();
            wp.JourneyId = journey.Id;
            _context.JourneyWaypoints.Add(wp);
        }
        await _context.SaveChangesAsync(cancellationToken);
        return journey;
    }

    public async Task<Journey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Journeys
            .Include(j => j.CurrentMoodFactor)
            .Include(j => j.JourneyWaypoints.OrderBy(w => w.StopOrder))
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task<List<Journey>> GetByTravelerIdAsync(Guid travelerId, CancellationToken cancellationToken = default)
    {
        return await _context.Journeys
            .Include(j => j.CurrentMoodFactor)
            .AsNoTracking()
            .Where(j => j.TravelerId == travelerId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
