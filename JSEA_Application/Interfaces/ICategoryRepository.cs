namespace JSEA_Application.Interfaces;

public interface ICategoryRepository
{
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<Models.Category>> GetActiveListAsync(CancellationToken cancellationToken = default);
    Task<Models.Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Models.Category> CreateAsync(Models.Category category, CancellationToken cancellationToken = default);
    Task UpdateAsync(Models.Category category, CancellationToken cancellationToken = default);
}