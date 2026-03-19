using JSEA_Application.Models;

namespace JSEA_Application.Interfaces;

public interface IPackageRepository
{
    Task<List<Package>> GetListAsync(bool? isActive, CancellationToken cancellationToken = default);
    Task<Package?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Package> CreateAsync(Package package, CancellationToken cancellationToken = default);
    Task UpdateAsync(Package package, CancellationToken cancellationToken = default);
}