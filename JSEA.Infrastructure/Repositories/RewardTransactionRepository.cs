using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using Microsoft.EntityFrameworkCore;

namespace JSEA_Infrastructure.Repositories;

public class RewardTransactionRepository : IRewardTransactionRepository
{
    private readonly AppDbContext _context;

    public RewardTransactionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RewardTransaction transaction, CancellationToken cancellationToken = default)
    {
        if (transaction.Id == Guid.Empty)
            transaction.Id = Guid.NewGuid();
        transaction.CreatedAt ??= DateTime.UtcNow;
        _context.RewardTransactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
