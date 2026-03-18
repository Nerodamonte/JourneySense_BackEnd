using JSEA_Application.DTOs.Respone.UserPackage;
using JSEA_Application.Interfaces;

namespace JSEA_Application.Services.UserPackage;

public class UserPackageService : IUserPackageService
{
    private readonly IUserPackageRepository _userPackageRepository;

    public UserPackageService(IUserPackageRepository userPackageRepository)
    {
        _userPackageRepository = userPackageRepository;
    }

    public async Task<UserCurrentPackageDto?> GetCurrentPackageByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var up = await _userPackageRepository.GetCurrentByUserIdAsync(userId, nowUtc, cancellationToken);
        if (up == null)
            return null;

        var isEffective = up.IsActive == true && (!up.ExpiresAt.HasValue || up.ExpiresAt.Value >= nowUtc);
        var status = isEffective ? "active" : "inactive";

        return new UserCurrentPackageDto
        {
            UserId = up.UserId,
            UserPackageId = up.Id,
            PackageId = up.PackageId,
            Name = up.Package.Title,
            Description = up.Package.Benefit,
            Price = up.Package.Price,
            SalePrice = up.Package.SalePrice,
            Type = up.Package.Type,
            StartDate = up.ActivatedAt,
            EndDate = up.ExpiresAt,
            Status = status,
            DistanceLimitKm = up.DistanceLimitKm,
            UsedKm = up.UsedKm,
            DurationInDays = up.Package.DurationInDays
        };
    }
}

