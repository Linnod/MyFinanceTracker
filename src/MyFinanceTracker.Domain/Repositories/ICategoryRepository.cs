using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.Domain.Repositories;

public interface ICategoryRepository
{
    Task<Category?> GetByAlias(string alias, CancellationToken ct);
    Task<IReadOnlyList<Category>> GetAll(CancellationToken ct);
}