using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.Domain.Repositories;

public interface ICategoryRepository
{
    Task<Category?> GetByAlias(string alias, CancellationToken ct = default);
    Task<IReadOnlyCollection<Category>> GetAll(CancellationToken ct = default);
}