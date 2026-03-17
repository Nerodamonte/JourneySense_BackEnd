using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Infrastructure.Repositories;

public class UserPackageRepository : IUserPackageRepository
{
    private readonly AppDbContext _context;

    public UserPackageRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserPackage?> GetCurrentByUserIdAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        return await _context.UserPackages
            .AsNoTracking()
            .Include(x => x.Package)
            .Where(x => x.UserId == userId)
            .Where(x => x.IsActive == true)
            .Where(x => !x.ExpiresAt.HasValue || x.ExpiresAt.Value >= nowUtc)
            .OrderByDescending(x => x.ActivatedAt ?? DateTime.MinValue)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}

