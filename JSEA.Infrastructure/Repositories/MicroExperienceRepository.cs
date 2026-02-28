using JSEA_Application.DTOs.Request.MicroExperience;
using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Infrastructure.Repositories;

public class MicroExperienceRepository : IMicroExperienceRepository
{
    private readonly AppDbContext _context;

    public MicroExperienceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<MicroExperience>> FindAllAsync(MicroExperienceFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _context.MicroExperiences.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim().ToLower();
            query = query.Where(x =>
                (x.Name != null && x.Name.ToLower().Contains(keyword)) ||
                (x.Address != null && x.Address.ToLower().Contains(keyword)) ||
                (x.City != null && x.City.ToLower().Contains(keyword)));
        }

        if (filter.CategoryId.HasValue)
            query = query.Where(x => x.CategoryId == filter.CategoryId.Value);

        if (filter.Status.HasValue)
            query = query.Where(x => x.Status == filter.Status.Value);

        return await query
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<MicroExperience?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.MicroExperiences
            .Include(x => x.Category)
            .Include(x => x.ExperienceDetail)
            .Include(x => x.ExperienceMetric)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.MicroExperiences
            .AnyAsync(x => x.Slug == slug, cancellationToken);
    }

    public async Task<MicroExperience> SaveAsync(MicroExperience entity, CancellationToken cancellationToken = default)
    {
        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            _context.MicroExperiences.Add(entity);
        }
        else
        {
            entity.UpdatedAt = DateTime.UtcNow;
            _context.MicroExperiences.Update(entity);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.MicroExperiences.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.MicroExperiences.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
