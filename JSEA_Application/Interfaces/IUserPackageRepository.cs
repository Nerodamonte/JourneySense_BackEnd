using JSEA_Application.Models;

namespace JSEA_Application.Interfaces;

public interface IUserPackageRepository
{
    Task<UserPackage?> GetCurrentByUserIdAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task<UserPackage> CreateAsync(UserPackage userPackage, CancellationToken cancellationToken = default);

    /// <summary>Cộng km đã đi vào gói đang active. Bỏ qua nếu delta ≤ 0 hoặc không có gói.</summary>
    Task AddUsedKmToActivePackageAsync(Guid userId, decimal deltaKm, DateTime nowUtc, CancellationToken cancellationToken = default);

    Task DeactivateCurrentAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default);
}

