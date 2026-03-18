using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Infrastructure.Repositories;

public class PackageRepository : IPackageRepository
{
    private readonly AppDbContext _context;

    public PackageRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Package>> GetListAsync(bool? isActive, CancellationToken cancellationToken = default)
    {
        var query = _context.Packages.AsNoTracking();
        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        return await query
            .OrderByDescending(p => p.IsPopular == true)
            .ThenBy(p => p.Price)
            .ToListAsync(cancellationToken);
    }

    public async Task<Package?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Packages.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Package> CreateAsync(Package package, CancellationToken cancellationToken = default)
    {
        if (package.Id == Guid.Empty)
            package.Id = Guid.NewGuid();
        if (!package.CreatedAt.HasValue)
            package.CreatedAt = DateTime.UtcNow;
        _context.Packages.Add(package);
        await _context.SaveChangesAsync(cancellationToken);
        return package;
    }

    public async Task UpdateAsync(Package package, CancellationToken cancellationToken = default)
    {
        _context.Packages.Update(package);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

