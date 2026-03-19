using JSEA_Application.Models;

namespace JSEA_Application.Interfaces;

public interface IUserPackageRepository
{
    Task<UserPackage?> GetCurrentByUserIdAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task<UserPackage> CreateAsync(UserPackage userPackage, CancellationToken cancellationToken = default);
    Task DeactivateCurrentAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default);
}

