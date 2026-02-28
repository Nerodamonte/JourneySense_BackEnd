namespace JSEA_Application.Interfaces;

public interface ICategoryRepository
{
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
