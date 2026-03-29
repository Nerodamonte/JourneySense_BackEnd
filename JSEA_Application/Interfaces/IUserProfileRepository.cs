using JSEA_Application.Models;

namespace JSEA_Application.Interfaces;

public interface IUserProfileRepository
{
    /// <summary>Ảnh đại diện theo userId (chỉ user có profile).</summary>
    Task<Dictionary<Guid, string?>> GetAvatarUrlsByUserIdsAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default);

    Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserProfile> CreateAsync(UserProfile profile, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserProfile profile, CancellationToken cancellationToken = default);
}
