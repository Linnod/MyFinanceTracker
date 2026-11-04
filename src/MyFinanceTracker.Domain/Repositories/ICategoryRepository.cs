using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.Domain.Repositories;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAll(CancellationToken ct);
    Task<Category?> Resolve(string query, CancellationToken ct);
}
