using MyFinanceTracker.Infrastructure.Persistence.Models;

namespace MyFinanceTracker.Infrastructure.Persistence.Repositories;

internal interface IRemoteCategoryRepository
{
    Task<IReadOnlyList<CategorySyncRecord>> GetAll(CancellationToken ct);
    Task Add(CategorySyncRecord category, CancellationToken ct);
    Task Update(CategorySyncRecord category, CancellationToken ct);
}
