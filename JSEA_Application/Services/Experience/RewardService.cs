using JSEA_Application.Interfaces;
using JSEA_Application.Models;

namespace JSEA_Application.Services.Experience;

public class RewardService : IRewardService
{
    private readonly IUserProfileRepository _userProfileRepository;

    public RewardService(IUserProfileRepository userProfileRepository)
    {
        _userProfileRepository = userProfileRepository;
    }

    public async Task AddRewardPointsAsync(Guid userId, int points, string reason, CancellationToken cancellationToken = default)
    {
        if (points <= 0) return;

        var profile = await _userProfileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile == null)
        {
            profile = new UserProfile { UserId = userId, RewardPoints = points };
            await _userProfileRepository.CreateAsync(profile, cancellationToken);
            return;
        }
        profile.RewardPoints += points;
        await _userProfileRepository.UpdateAsync(profile, cancellationToken);
    }
}
