using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.Domain.Repositories;

public interface ICategoryRepository
{
    Task<Category?> GetByAlias(string alias, CancellationToken ct = default);
    Task<IReadOnlyCollection<string>> GetAllAliases(CancellationToken ct = default);
}