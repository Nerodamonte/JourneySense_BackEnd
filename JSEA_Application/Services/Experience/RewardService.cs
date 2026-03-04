using JSEA_Application.Interfaces;

namespace JSEA_Application.Services.Experience;

/// <summary>Schema v10 không có reward_points trong user_profiles. Service này tạm no-op.</summary>
public class RewardService : IRewardService
{
    public Task AddRewardPointsAsync(Guid userId, int points, string reason, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
