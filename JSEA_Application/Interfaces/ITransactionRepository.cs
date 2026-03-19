using JSEA_Application.Models;

namespace JSEA_Application.Interfaces;

public interface ITransactionRepository
{
    Task<Transaction> CreateAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task<Transaction?> GetByOrderCodeAsync(string orderCode, CancellationToken cancellationToken = default);
    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);
}
