using JSEA_Application.Constants;
using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Infrastructure.Repositories;

public class FeedbackRepository : IFeedbackRepository
{
    private readonly AppDbContext _context;

    public FeedbackRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Feedback> SaveAsync(Feedback feedback, CancellationToken cancellationToken = default)
    {
        if (feedback.Id == Guid.Empty)
        {
            feedback.Id = Guid.NewGuid();
            feedback.CreatedAt ??= DateTime.UtcNow;
            _context.Feedbacks.Add(feedback);
        }
        else
        {
            _context.Feedbacks.Update(feedback);
        }
        await _context.SaveChangesAsync(cancellationToken);
        return feedback;
    }

    public async Task<List<string>> GetTopByExperienceIdAsync(Guid experienceId, int topN, CancellationToken cancellationToken = default)
    {
        return await _context.Feedbacks
            .AsNoTracking()
            .Include(f => f.Visit)
            .Where(f =>
                f.Visit.ExperienceId == experienceId &&
                (f.ModerationStatus ?? "").ToLower() == FeedbackModerationStatuses.Approved &&
                f.IsFlagged != true)
            .OrderByDescending(f => f.CreatedAt)
            .Take(topN)
            .Select(f => f.FeedbackText)
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<Feedback> Items, int TotalCount)> ListPublicApprovedForExperienceAsync(
        Guid experienceId,
        Guid? excludeTravelerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var q = _context.Feedbacks
            .AsNoTracking()
            .Include(f => f.Visit)
                .ThenInclude(v => v.Traveler)
                    .ThenInclude(t => t.UserProfile)
            .Include(f => f.Visit!)
                .ThenInclude(v => v.Rating)
            .Where(f =>
                f.Visit!.ExperienceId == experienceId &&
                (f.ModerationStatus ?? "").ToLower() == FeedbackModerationStatuses.Approved &&
                f.IsFlagged != true);

        if (excludeTravelerId.HasValue)
            q = q.Where(f => f.Visit!.TravelerId != excludeTravelerId.Value);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<Feedback?> GetByVisitIdAsync(Guid visitId, CancellationToken cancellationToken = default)
    {
        return await _context.Feedbacks
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.VisitId == visitId, cancellationToken);
    }

    public async Task<Feedback?> GetByIdWithVisitAsync(Guid feedbackId, CancellationToken cancellationToken = default)
    {
        return await _context.Feedbacks
            .AsNoTracking()
            .Include(f => f.Visit)
            .ThenInclude(v => v.Experience)
            .Include(f => f.Visit)
            .ThenInclude(v => v.Traveler)
            .Include(f => f.Visit)
            .ThenInclude(v => v.Journey)
            .FirstOrDefaultAsync(f => f.Id == feedbackId, cancellationToken);
    }

    public async Task<(List<Feedback> Items, int TotalCount)> ListForStaffAsync(
        string? moderationStatus,
        Guid? experienceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = _context.Feedbacks
            .AsNoTracking()
            .Include(f => f.Visit)
            .ThenInclude(v => v.Experience)
            .Include(f => f.Visit)
            .ThenInclude(v => v.Traveler)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(moderationStatus))
            q = q.Where(f => f.ModerationStatus == moderationStatus.Trim().ToLowerInvariant());

        if (experienceId.HasValue)
            q = q.Where(f => f.Visit.ExperienceId == experienceId.Value);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<bool> TryModerateAsync(
        Guid feedbackId,
        string moderationStatus,
        bool isFlagged,
        string? flaggedReason,
        CancellationToken cancellationToken = default)
    {
        var f = await _context.Feedbacks.FirstOrDefaultAsync(x => x.Id == feedbackId, cancellationToken);
        if (f == null)
            return false;

        f.ModerationStatus = moderationStatus;
        f.IsFlagged = isFlagged;
        f.FlaggedReason = flaggedReason;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
