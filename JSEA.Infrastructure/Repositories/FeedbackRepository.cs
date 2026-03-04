using JSEA_Application.Interfaces;
using JSEA_Application.Models;

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
}
