using MyFinanceTracker.Infrastructure.Persistence.Models;

namespace MyFinanceTracker.Infrastructure.Persistence.Repositories;

internal interface IRemoteTransactionRepository
{
    Task<IReadOnlyList<TransactionSyncRecord>> GetAll(CancellationToken ct);
    Task AddRange(IReadOnlyCollection<TransactionSyncRecord> transactions, CancellationToken ct);
    Task Update(TransactionSyncRecord transaction, CancellationToken ct);
    Task<int> DeleteRange(IReadOnlyCollection<Guid> ids, CancellationToken ct);
}
