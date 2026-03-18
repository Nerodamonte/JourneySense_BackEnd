using JSEA_Application.Models;

namespace JSEA_Application.Interfaces;

public interface IRatingRepository
{
    Task<Rating> SaveAsync(Rating rating, CancellationToken cancellationToken = default);
    Task<Rating?> GetByVisitIdAsync(Guid visitId, CancellationToken cancellationToken = default);
}
