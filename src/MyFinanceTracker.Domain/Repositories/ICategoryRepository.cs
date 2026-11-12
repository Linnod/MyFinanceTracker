using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.Domain.Repositories;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAll(CancellationToken ct);
    Task<Category?> GetById(string id, CancellationToken ct);
    Task<IReadOnlyList<Category>> GetByIds(IReadOnlyCollection<string> ids, CancellationToken ct);
    Task Add(Category category, CancellationToken ct);
    Task Update(Category category, CancellationToken ct);
}
