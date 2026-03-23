using JSEA_Application.Interfaces;
using JSEA_Application.Models;

namespace JSEA_Application.Services.Experience;

public class RewardService : IRewardService
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IRewardTransactionRepository _rewardTransactionRepository;

    public RewardService(
        IUserProfileRepository userProfileRepository,
        IRewardTransactionRepository rewardTransactionRepository)
    {
        _userProfileRepository = userProfileRepository;
        _rewardTransactionRepository = rewardTransactionRepository;
    }

    public async Task AddRewardPointsAsync(
        Guid userId,
        int points,
        string reason,
        CancellationToken cancellationToken = default,
        Guid? achievementId = null,
        Guid? refId = null,
        string? refType = null)
    {
        if (points <= 0)
            return;

        var profile = await _userProfileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile == null)
        {
            profile = new UserProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RewardPoints = points
            };
            await _userProfileRepository.CreateAsync(profile, cancellationToken);
        }
        else
        {
            profile.RewardPoints += points;
            await _userProfileRepository.UpdateAsync(profile, cancellationToken);
        }

        await _rewardTransactionRepository.AddAsync(new RewardTransaction
        {
            UserId = userId,
            Type = "earned",
            Points = points,
            Description = reason,
            AchievementId = achievementId,
            RefId = refId,
            RefType = refType
        }, cancellationToken);
    }
}
