using MyFinanceTracker.Domain.Common;
using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.Domain.Repositories;

public interface ITransactionRepository
{
    Task AddRange(IReadOnlyCollection<Transaction> transactions, CancellationToken ct);
    Task<int> DeleteRange(IReadOnlyCollection<Guid> ids, CancellationToken ct);

    Task<Transaction?> GetById(Guid id, CancellationToken ct);
    Task Update(Transaction transaction, CancellationToken ct);

    Task<IReadOnlyList<Transaction>> GetAll(CancellationToken ct);

    Task<PagedResult<Transaction>> Get(TransactionFilter filter, CancellationToken ct);
}
