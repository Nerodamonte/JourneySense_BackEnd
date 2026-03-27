namespace JSEA_Application.Interfaces;

public interface IRewardService
{
    Task<int> GetRewardPointsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddRewardPointsAsync(
        Guid userId,
        int points,
        string reason,
        CancellationToken cancellationToken = default,
        Guid? achievementId = null,
        Guid? refId = null,
        string? refType = null);

    Task SubtractRewardPointsAsync(
        Guid userId,
        int points,
        string reason,
        CancellationToken cancellationToken = default,
        Guid? refId = null,
        string? refType = null);
}
