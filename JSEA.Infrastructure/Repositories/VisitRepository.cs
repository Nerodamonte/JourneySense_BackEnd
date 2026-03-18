using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Infrastructure.Repositories;

public class VisitRepository : IVisitRepository
{
    private readonly AppDbContext _context;

    public VisitRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsVisitAsync(Guid travelerId, Guid experienceId, Guid journeyId, CancellationToken cancellationToken = default)
    {
        return await _context.Visits
            .AnyAsync(v =>
                v.TravelerId == travelerId &&
                v.ExperienceId == experienceId &&
                v.JourneyId == journeyId,
                cancellationToken);
    }

    public async Task<Visit> SaveAsync(Visit visit, CancellationToken cancellationToken = default)
    {
        if (visit.Id == Guid.Empty)
        {
            visit.Id = Guid.NewGuid();
            visit.VisitedAt ??= DateTime.UtcNow;
            _context.Visits.Add(visit);
        }
        else
        {
            _context.Visits.Update(visit);
        }
        await _context.SaveChangesAsync(cancellationToken);
        return visit;
    }

    public async Task<Visit?> GetByJourneyTravelerExperienceAsync(
        Guid journeyId,
        Guid travelerId,
        Guid experienceId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Visits
            .FirstOrDefaultAsync(v =>
                v.JourneyId == journeyId &&
                v.TravelerId == travelerId &&
                v.ExperienceId == experienceId,
                cancellationToken);
    }
}
