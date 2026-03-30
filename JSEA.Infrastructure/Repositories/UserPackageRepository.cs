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

    public async Task<UserPackage> CreateAsync(UserPackage userPackage, CancellationToken cancellationToken = default)
    {
        _context.UserPackages.Add(userPackage);
        await _context.SaveChangesAsync(cancellationToken);
        return userPackage;
    }

    public async Task AddUsedKmToActivePackageAsync(
        Guid userId,
        decimal deltaKm,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        if (deltaKm <= 0)
            return;

        var package = await _context.UserPackages
            .Where(x => x.UserId == userId)
            .Where(x => x.IsActive == true)
            .Where(x => !x.ExpiresAt.HasValue || x.ExpiresAt.Value >= nowUtc)
            .OrderByDescending(x => x.ActivatedAt ?? DateTime.MinValue)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (package == null)
            return;

        package.UsedKm += deltaKm;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateCurrentAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var activePackages = await _context.UserPackages
            .Where(x => x.UserId == userId)
            .Where(x => x.IsActive == true)
            .Where(x => !x.ExpiresAt.HasValue || x.ExpiresAt.Value >= nowUtc)
            .ToListAsync(cancellationToken);

        foreach (var up in activePackages)
            up.IsActive = false;

        await _context.SaveChangesAsync(cancellationToken);
    }
}

