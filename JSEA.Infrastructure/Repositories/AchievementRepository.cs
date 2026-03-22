using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Infrastructure.Repositories;

public class AchievementRepository : IAchievementRepository
{
    private readonly AppDbContext _context;

    public AchievementRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Achievement?> GetActiveByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Achievements
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Code == code && a.IsActive, cancellationToken);
    }
}
