using JSEA_Application.Constants;
using JSEA_Application.DTOs.Respone.Journey;
using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Infrastructure.Repositories;

public class JourneyMemberRepository : IJourneyMemberRepository
{
    private readonly AppDbContext _context;

    public JourneyMemberRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<JourneyMember?> GetActiveByTravelerAsync(Guid journeyId, Guid travelerId, CancellationToken cancellationToken = default) =>
        await _context.JourneyMembers
            .FirstOrDefaultAsync(m => m.JourneyId == journeyId && m.TravelerId == travelerId && m.IsActive, cancellationToken);

    public async Task<JourneyMember?> GetActiveByGuestKeyAsync(Guid journeyId, Guid guestKey, CancellationToken cancellationToken = default) =>
        await _context.JourneyMembers
            .FirstOrDefaultAsync(m => m.JourneyId == journeyId && m.GuestKey == guestKey && m.IsActive, cancellationToken);

    public async Task<IReadOnlyList<JourneyMember>> GetActiveMembersAsync(Guid journeyId, CancellationToken cancellationToken = default) =>
        await _context.JourneyMembers
            .AsNoTracking()
            .Where(m => m.JourneyId == journeyId && m.IsActive)
            .ToListAsync(cancellationToken);

    public async Task<JourneyMember> AddAsync(JourneyMember member, CancellationToken cancellationToken = default)
    {
        if (member.Id == Guid.Empty)
            member.Id = Guid.NewGuid();
        member.JoinedAt = member.JoinedAt == default ? DateTime.UtcNow : member.JoinedAt;
        _context.JourneyMembers.Add(member);
        await _context.SaveChangesAsync(cancellationToken);
        return member;
    }

    public async Task<JourneyMember> UpdateAsync(JourneyMember member, CancellationToken cancellationToken = default)
    {
        _context.JourneyMembers.Update(member);
        await _context.SaveChangesAsync(cancellationToken);
        return member;
    }

    public async Task<JourneyMember> EnsureOwnerMemberAsync(
        Guid journeyId,
        Guid ownerTravelerId,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.JourneyMembers
            .FirstOrDefaultAsync(m =>
                m.JourneyId == journeyId &&
                m.TravelerId == ownerTravelerId &&
                m.IsActive &&
                m.Role == JourneyMemberRoles.Owner, cancellationToken);
        if (existing != null)
            return existing;

        var member = new JourneyMember
        {
            JourneyId = journeyId,
            TravelerId = ownerTravelerId,
            DisplayName = displayName.Trim(),
            IsRegisteredUser = true,
            Role = JourneyMemberRoles.Owner,
            IsActive = true,
            JoinedAt = DateTime.UtcNow
        };
        return await AddAsync(member, cancellationToken);
    }

    public async Task<bool> IsSoloOwnerOnlyActiveRosterAsync(Guid journeyId, CancellationToken cancellationToken = default)
    {
        var roles = await _context.JourneyMembers.AsNoTracking()
            .Where(m => m.JourneyId == journeyId && m.IsActive)
            .Select(m => m.Role)
            .ToListAsync(cancellationToken);

        if (roles.Count == 0)
            return true;

        if (roles.Count > 1)
            return false;

        return string.Equals(roles[0], JourneyMemberRoles.Owner, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> AllActiveMembersConfirmedAtDestinationAsync(Guid journeyId, CancellationToken cancellationToken = default)
    {
        var members = await _context.JourneyMembers.AsNoTracking()
            .Where(m => m.JourneyId == journeyId && m.IsActive)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        if (members.Count == 0)
            return false;

        var progress = await _context.JourneyWaypointMemberProgresses.AsNoTracking()
            .Where(p => members.Contains(p.JourneyMemberId))
            .ToListAsync(cancellationToken);

        var byMember = progress.GroupBy(p => p.JourneyMemberId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var memberId in members)
        {
            if (!byMember.TryGetValue(memberId, out var list))
                list = new List<JourneyWaypointMemberProgress>();

            var dest = list.FirstOrDefault(p => p.MilestoneKind == JourneyMilestoneKinds.Destination);
            if (dest == null)
                return false;

            // Nút "đã tới đích" = destination check-in (ArrivedAt); có thể checkout sau (DepartedAt); hoặc skip đích.
            var ok = dest.ArrivedAt.HasValue || dest.DepartedAt.HasValue || dest.Skipped;
            if (!ok)
                return false;
        }

        return true;
    }

    public async Task<JourneyWaypointMemberProgress?> GetProgressAsync(
        Guid journeyMemberId,
        Guid? journeyWaypointId,
        string milestoneKind,
        CancellationToken cancellationToken = default)
    {
        if (milestoneKind == JourneyMilestoneKinds.Destination)
            return await _context.JourneyWaypointMemberProgresses
                .FirstOrDefaultAsync(p =>
                    p.JourneyMemberId == journeyMemberId && p.MilestoneKind == JourneyMilestoneKinds.Destination, cancellationToken);

        return await _context.JourneyWaypointMemberProgresses
            .FirstOrDefaultAsync(p =>
                p.JourneyMemberId == journeyMemberId &&
                p.JourneyWaypointId == journeyWaypointId &&
                p.MilestoneKind == JourneyMilestoneKinds.Waypoint, cancellationToken);
    }

    public async Task<JourneyWaypointMemberProgress> SaveProgressAsync(
        JourneyWaypointMemberProgress progress,
        CancellationToken cancellationToken = default)
    {
        if (progress.Id == Guid.Empty)
        {
            progress.Id = Guid.NewGuid();
            _context.JourneyWaypointMemberProgresses.Add(progress);
        }
        else
        {
            _context.JourneyWaypointMemberProgresses.Update(progress);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return progress;
    }

    public async Task<List<JourneyWaypointMemberProgress>> GetProgressForMemberAsync(
        Guid journeyMemberId,
        CancellationToken cancellationToken = default) =>
        await _context.JourneyWaypointMemberProgresses
            .AsNoTracking()
            .Where(p => p.JourneyMemberId == journeyMemberId)
            .ToListAsync(cancellationToken);

    public async Task<JourneyWaypointAttendanceResponse> GetWaypointAttendanceAsync(
        Guid journeyId,
        CancellationToken cancellationToken = default)
    {
        var activeMemberIds = await _context.JourneyMembers.AsNoTracking()
            .Where(m => m.JourneyId == journeyId && m.IsActive)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);
        var activeCount = activeMemberIds.Count;

        var waypoints = await _context.JourneyWaypoints.AsNoTracking()
            .Where(w => w.JourneyId == journeyId)
            .OrderBy(w => w.StopOrder)
            .Select(w => new { w.Id, w.StopOrder })
            .ToListAsync(cancellationToken);

        if (activeCount == 0)
        {
            return new JourneyWaypointAttendanceResponse
            {
                JourneyId = journeyId,
                ActiveMemberCount = 0,
                Waypoints = waypoints
                    .Select(w => new JourneyWaypointAttendanceItemResponse
                    {
                        WaypointId = w.Id,
                        StopOrder = w.StopOrder,
                        ArrivedCount = 0
                    })
                    .ToList()
            };
        }

        var progressed = await _context.JourneyWaypointMemberProgresses.AsNoTracking()
            .Where(p => activeMemberIds.Contains(p.JourneyMemberId))
            .Where(p => p.MilestoneKind == JourneyMilestoneKinds.Waypoint && p.JourneyWaypointId != null)
            .Where(p => p.ArrivedAt != null || p.DepartedAt != null || p.Skipped)
            .Select(p => new { WpId = p.JourneyWaypointId!.Value, p.JourneyMemberId })
            .ToListAsync(cancellationToken);

        var counts = progressed
            .GroupBy(x => x.WpId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.JourneyMemberId).Distinct().Count());

        return new JourneyWaypointAttendanceResponse
        {
            JourneyId = journeyId,
            ActiveMemberCount = activeCount,
            Waypoints = waypoints.Select(w => new JourneyWaypointAttendanceItemResponse
            {
                WaypointId = w.Id,
                StopOrder = w.StopOrder,
                ArrivedCount = counts.TryGetValue(w.Id, out var c) ? c : 0
            }).ToList()
        };
    }
}
