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
            .Where(f => f.Visit.ExperienceId == experienceId && f.IsFlagged != true)
            .OrderByDescending(f => f.CreatedAt)
            .Take(topN)
            .Select(f => f.FeedbackText)
            .ToListAsync(cancellationToken);
    }
}
