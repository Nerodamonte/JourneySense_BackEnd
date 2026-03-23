using JSEA_Application.Models;

namespace JSEA_Application.Interfaces;

public interface IRewardTransactionRepository
{
    Task AddAsync(RewardTransaction transaction, CancellationToken cancellationToken = default);
}
