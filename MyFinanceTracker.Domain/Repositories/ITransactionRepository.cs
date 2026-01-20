using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.Domain.Repositories;

public interface ITransactionRepository
{
    Task Add(Transaction transaction, CancellationToken ct = default);
    
    Task AddRange(IEnumerable<Transaction> transactions, CancellationToken ct = default);
}