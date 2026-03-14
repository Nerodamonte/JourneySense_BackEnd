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

    public async Task<Journey> SaveAsync(Journey journey, List<JourneyWaypoint> waypoints, List<RouteSegment>? segments = null, CancellationToken cancellationToken = default)
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

        if (segments != null && segments.Any())
        {
            foreach (var seg in segments)
            {
                if (seg.Id == Guid.Empty)
                    seg.Id = Guid.NewGuid();

                seg.JourneyId = journey.Id;

                _context.RouteSegments.Add(seg);
            }
        }
        await _context.SaveChangesAsync(cancellationToken);
        return journey;
    }

    public async Task<Journey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Journeys
            .Include(j => j.JourneyWaypoints.OrderBy(w => w.StopOrder))
                .Include(j => j.RouteSegments.OrderBy(s => s.SegmentOrder))
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task<List<Journey>> GetByTravelerIdAsync(Guid travelerId, CancellationToken cancellationToken = default)
    {
        return await _context.Journeys
            .AsNoTracking()
            .Where(j => j.TravelerId == travelerId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Guid>> GetSuggestedExperienceIdsAsync(Guid journeyId, CancellationToken cancellationToken = default)
    {
        return await _context.JourneySuggestions
            .AsNoTracking()
            .Where(s => s.JourneyId == journeyId)
            .Select(s => s.ExperienceId)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUsedMinutesAsync(Guid journeyId, CancellationToken cancellationToken = default)
    {
        return await _context.JourneyWaypoints
            .AsNoTracking()
            .Where(w => w.JourneyId == journeyId)
            .Include(w => w.Suggestion)
            .SumAsync(w =>
                (w.PlannedStopMinutes ?? 0) +
                (w.Suggestion != null ? w.Suggestion.DetourTimeMinutes ?? 0 : 0),
                cancellationToken);
    }

    public async Task<int> GetAcceptedWaypointCountAsync(Guid journeyId, CancellationToken cancellationToken = default)
    {
        return await _context.JourneyWaypoints
            .AsNoTracking()
            .CountAsync(w => w.JourneyId == journeyId, cancellationToken);
    }

    public async Task<JourneySuggestion> SaveSuggestionAsync(JourneySuggestion suggestion, CancellationToken cancellationToken = default)
    {
        if (suggestion.Id == Guid.Empty)
            suggestion.Id = Guid.NewGuid();

        _context.JourneySuggestions.Add(suggestion);
        await _context.SaveChangesAsync(cancellationToken);
        return suggestion;
    }

    public async Task<JourneySuggestion?> GetSuggestionByIdAsync(Guid suggestionId, CancellationToken cancellationToken = default)
    {
        return await _context.JourneySuggestions
            .Include(s => s.Journey)
            .Include(s => s.Experience)
                .ThenInclude(e => e.ExperienceDetail)
            .Include(s => s.Experience)
                .ThenInclude(e => e.ExperienceMetric)
            .FirstOrDefaultAsync(s => s.Id == suggestionId, cancellationToken);
    }

    public async Task UpdateSuggestionInsightAsync(Guid suggestionId, string insight, CancellationToken cancellationToken = default)
    {
        var suggestion = await _context.JourneySuggestions
            .FirstOrDefaultAsync(s => s.Id == suggestionId, cancellationToken);

        if (suggestion == null) return;

        suggestion.AiInsight = insight;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
