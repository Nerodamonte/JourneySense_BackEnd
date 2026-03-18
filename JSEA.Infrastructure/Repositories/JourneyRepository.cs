using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using JSEA_Application.Enums;
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
                .ThenInclude(w => w.Experience)
                    .ThenInclude(e => e.Category)
            .Include(j => j.JourneyWaypoints.OrderBy(w => w.StopOrder))
                .ThenInclude(w => w.Experience)
                    .ThenInclude(e => e.ExperiencePhotos)
            .Include(j => j.JourneyWaypoints.OrderBy(w => w.StopOrder))
                .ThenInclude(w => w.Suggestion)
            .Include(j => j.RouteSegments.OrderBy(s => s.SegmentOrder))
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task<Journey?> GetBasicByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Journeys
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task<JourneyWaypoint?> GetWaypointForTravelerAsync(
        Guid journeyId,
        Guid waypointId,
        Guid travelerId,
        CancellationToken cancellationToken = default)
    {
        return await _context.JourneyWaypoints
            .Include(w => w.Journey)
            .FirstOrDefaultAsync(w =>
                w.Id == waypointId &&
                w.JourneyId == journeyId &&
                w.Journey.TravelerId == travelerId,
                cancellationToken);
    }

    public async Task<Journey> UpdateAsync(Journey journey, CancellationToken cancellationToken = default)
    {
        _context.Journeys.Update(journey);
        await _context.SaveChangesAsync(cancellationToken);
        return journey;
    }

    public async Task<JourneyWaypoint> UpdateWaypointAsync(JourneyWaypoint waypoint, CancellationToken cancellationToken = default)
    {
        _context.JourneyWaypoints.Update(waypoint);
        await _context.SaveChangesAsync(cancellationToken);
        return waypoint;
    }

    public async Task<List<Journey>> GetByTravelerIdAsync(Guid travelerId, CancellationToken cancellationToken = default)
    {
        return await _context.Journeys
            .AsNoTracking()
            .Where(j => j.TravelerId == travelerId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Guid>> GetSuggestedExperienceIdsAsync(Guid journeyId, Guid segmentId, CancellationToken cancellationToken = default)
    {
        return await _context.JourneySuggestions
            .AsNoTracking()
            .Where(s => s.JourneyId == journeyId && s.SegmentId == segmentId)
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

    public async Task SaveSuggestionsAsync(IEnumerable<JourneySuggestion> suggestions, CancellationToken cancellationToken = default)
    {
        var list = suggestions.ToList();
        if (list.Count == 0) return;

        foreach (var s in list)
        {
            if (s.Id == Guid.Empty)
                s.Id = Guid.NewGuid();
        }

        _context.JourneySuggestions.AddRange(list);
        await _context.SaveChangesAsync(cancellationToken);
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

    public async Task<List<JourneySuggestion>> GetSuggestionsByIdsAsync(IEnumerable<Guid> suggestionIds, CancellationToken cancellationToken = default)
    {
        var ids = suggestionIds.Distinct().ToList();
        if (ids.Count == 0) return new List<JourneySuggestion>();

        return await _context.JourneySuggestions
            .AsNoTracking()
            .Where(s => ids.Contains(s.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<JourneySuggestion>> GetSuggestionsByJourneySegmentAsync(
        Guid journeyId,
        Guid segmentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.JourneySuggestions
            .AsNoTracking()
            .Where(s => s.JourneyId == journeyId && s.SegmentId == segmentId)
            .Include(s => s.Experience)
                .ThenInclude(e => e.Category)
            .Include(s => s.Experience)
                .ThenInclude(e => e.ExperienceDetail)
            .Include(s => s.Experience)
                .ThenInclude(e => e.ExperienceMetric)
            .Include(s => s.Experience)
                .ThenInclude(e => e.ExperiencePhotos)
            .OrderByDescending(s => s.FinalSimilarity ?? 0)
            .ThenByDescending(s => s.SuggestedAt ?? DateTime.MinValue)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateSuggestionInsightAsync(Guid suggestionId, string insight, CancellationToken cancellationToken = default)
    {
        var suggestion = await _context.JourneySuggestions
            .FirstOrDefaultAsync(s => s.Id == suggestionId, cancellationToken);

        if (suggestion == null) return;

        suggestion.AiInsight = insight;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceWaypointsAsync(
        Guid journeyId,
        IEnumerable<JourneyWaypoint> newWaypoints,
        IEnumerable<SuggestionInteraction>? newInteractions = null,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.JourneyWaypoints
            .Where(w => w.JourneyId == journeyId)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
            _context.JourneyWaypoints.RemoveRange(existing);

        var wpList = newWaypoints.ToList();
        foreach (var wp in wpList)
        {
            if (wp.Id == Guid.Empty)
                wp.Id = Guid.NewGuid();
            wp.JourneyId = journeyId;
        }

        if (wpList.Count > 0)
            _context.JourneyWaypoints.AddRange(wpList);

        if (newInteractions != null)
        {
            var interList = newInteractions.ToList();
            foreach (var i in interList)
            {
                if (i.Id == Guid.Empty)
                    i.Id = Guid.NewGuid();
                i.InteractedAt ??= DateTime.UtcNow;
            }

            if (interList.Count > 0)
                _context.SuggestionInteractions.AddRange(interList);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddSuggestionInteractionAsync(Guid suggestionId, InteractionType interactionType, CancellationToken cancellationToken = default)
    {
        _context.SuggestionInteractions.Add(new SuggestionInteraction
        {
            Id = Guid.NewGuid(),
            SuggestionId = suggestionId,
            InteractionType = interactionType,
            InteractedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<Guid>> GetInteractionSuggestionIdsAsync(
        IEnumerable<Guid> suggestionIds,
        InteractionType interactionType,
        CancellationToken cancellationToken = default)
    {
        var ids = suggestionIds.Distinct().ToList();
        if (ids.Count == 0) return new List<Guid>();

        return await _context.SuggestionInteractions
     .AsNoTracking()
     .Where(i =>
         i.SuggestionId.HasValue &&
         ids.Contains(i.SuggestionId.Value) &&
         i.InteractionType == interactionType)
     .Select(i => i.SuggestionId.Value)
     .Distinct()
     .ToListAsync(cancellationToken);
    }
}