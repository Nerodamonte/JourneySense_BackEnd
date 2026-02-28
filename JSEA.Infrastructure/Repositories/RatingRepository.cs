using JSEA_Application.Interfaces;
using JSEA_Application.Models;

namespace JSEA_Infrastructure.Repositories;

public class RatingRepository : IRatingRepository
{
    private readonly AppDbContext _context;

    public RatingRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Rating> SaveAsync(Rating rating, CancellationToken cancellationToken = default)
    {
        if (rating.Id == Guid.Empty)
        {
            rating.Id = Guid.NewGuid();
            rating.CreatedAt ??= DateTime.UtcNow;
            _context.Ratings.Add(rating);
        }
        else
        {
            _context.Ratings.Update(rating);
        }
        await _context.SaveChangesAsync(cancellationToken);
        return rating;
    }
}
