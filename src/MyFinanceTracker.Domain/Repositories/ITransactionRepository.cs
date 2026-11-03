using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.Domain.Repositories;

public interface ITransactionRepository
{
    Task AddRange(IEnumerable<Transaction> transactions, CancellationToken ct);
    Task<DeleteTransactionsResult> DeleteRange(IReadOnlyCollection<Guid> ids, CancellationToken ct);

    Task<IReadOnlyList<Transaction>> Get(TransactionFilter filter, CancellationToken ct);
}