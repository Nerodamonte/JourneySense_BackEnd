namespace JSEA_Application.Interfaces;

public interface IRewardService
{
    Task AddRewardPointsAsync(Guid userId, int points, string reason, CancellationToken cancellationToken = default);
}
