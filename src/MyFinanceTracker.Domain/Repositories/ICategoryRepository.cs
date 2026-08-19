using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.Domain.Repositories;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAll(CancellationToken ct);
}