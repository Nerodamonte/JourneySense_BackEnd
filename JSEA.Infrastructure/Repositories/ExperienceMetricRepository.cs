using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Infrastructure.Repositories;

public class ExperienceMetricRepository : IExperienceMetricRepository
{
    private readonly AppDbContext _context;

    public ExperienceMetricRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task IncrementVisitCountAsync(Guid experienceId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var m = await _context.ExperienceMetrics
            .FirstOrDefaultAsync(x => x.ExperienceId == experienceId, cancellationToken);

        if (m == null)
        {
            m = new ExperienceMetric
            {
                ExperienceId = experienceId,
                QualityScore = 0,
                TotalVisits = 1,
                TotalRatings = 0,
                AvgRating = null,
                LastVisitedAt = now,
                UpdatedAt = now
            };
            _context.ExperienceMetrics.Add(m);
        }
        else
        {
            m.TotalVisits = (m.TotalVisits ?? 0) + 1;
            m.LastVisitedAt = now;
            m.UpdatedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRatingAsync(Guid experienceId, int stars1To5, CancellationToken cancellationToken = default)
    {
        stars1To5 = Math.Clamp(stars1To5, 1, 5);
        var now = DateTime.UtcNow;

        var m = await _context.ExperienceMetrics
            .FirstOrDefaultAsync(x => x.ExperienceId == experienceId, cancellationToken);

        var oldCount = m?.TotalRatings ?? 0;
        var newCount = oldCount + 1;
        var sum = (m?.AvgRating ?? 0m) * oldCount + stars1To5;
        var newAvg = Math.Round(sum / newCount, 1, MidpointRounding.AwayFromZero);

        if (m == null)
        {
            m = new ExperienceMetric
            {
                ExperienceId = experienceId,
                QualityScore = 0,
                TotalVisits = 0,
                TotalRatings = newCount,
                AvgRating = newAvg,
                UpdatedAt = now
            };
            _context.ExperienceMetrics.Add(m);
        }
        else
        {
            m.TotalRatings = newCount;
            m.AvgRating = newAvg;
            m.UpdatedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
