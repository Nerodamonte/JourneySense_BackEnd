using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Infrastructure.Repositories;

public class FactorRepository : IFactorRepository
{
    private readonly AppDbContext _context;

    public FactorRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Factor?> GetMoodFactorByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var normalized = name.Trim();
        return await _context.Factors
            .AsNoTracking()
            .FirstOrDefaultAsync(f =>
                f.Type == "mood" &&
                f.Name.ToLower() == normalized.ToLower(),
                cancellationToken);
    }
}

