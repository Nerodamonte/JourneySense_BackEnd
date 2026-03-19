using JSEA_Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _context;

    public CategoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Categories.AnyAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<List<JSEA_Application.Models.Category>> GetActiveListAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive == true)
            .OrderBy(c => c.DisplayOrder ?? int.MaxValue)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<JSEA_Application.Models.Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<JSEA_Application.Models.Category> CreateAsync(JSEA_Application.Models.Category category, CancellationToken cancellationToken = default)
    {
        if (category.Id == Guid.Empty)
            category.Id = Guid.NewGuid();
        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);
        return category;
    }

    public async Task UpdateAsync(JSEA_Application.Models.Category category, CancellationToken cancellationToken = default)
    {
        _context.Categories.Update(category);
        await _context.SaveChangesAsync(cancellationToken);
    }
}