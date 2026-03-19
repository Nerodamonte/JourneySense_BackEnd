using JSEA_Application.DTOs.Respone.UserPackage;

namespace JSEA_Application.Interfaces;

public interface IUserPackageService
{
    Task<UserCurrentPackageDto?> GetCurrentPackageByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}