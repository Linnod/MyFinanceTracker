using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.Domain.Repositories;

public interface ITransactionRepository
{
    Task AddRange(IReadOnlyCollection<Transaction> transactions, CancellationToken ct);
    Task<int> DeleteRange(IReadOnlyCollection<Guid> ids, CancellationToken ct);

    Task<IReadOnlyList<Transaction>> Get(TransactionFilter filter, CancellationToken ct);
}