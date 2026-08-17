using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.Domain.Repositories;

public interface ITransactionRepository
{
    Task AddRange(IEnumerable<Transaction> transactions, CancellationToken ct);
    Task DeleteRange(Category category, DateOnly date, CancellationToken ct);

    Task<IReadOnlyList<Transaction>> Get(Category category, DateOnly date, CancellationToken ct);
}