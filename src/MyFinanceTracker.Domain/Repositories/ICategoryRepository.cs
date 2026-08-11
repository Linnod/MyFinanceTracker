using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.Domain.Repositories;

public interface ICategoryRepository
{
    Task<Category?> GetByAlias(string alias, CancellationToken ct = default);
    Task<IReadOnlyList<Category>> GetAll(CancellationToken ct = default);
}