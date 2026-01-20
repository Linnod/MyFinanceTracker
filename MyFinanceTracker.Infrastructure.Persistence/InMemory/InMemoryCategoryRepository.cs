using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;

namespace MyFinanceTracker.Infrastructure.Persistence.InMemory;

internal class InMemoryCategoryRepository : ICategoryRepository
{
    private readonly List<Category> categories =
    [
        new Category("B", "Доход", ["income"], IsIncome: true),
        new Category("C", "Продукты", ["food", "products"]),
        new Category("G", "Общественный", ["bus", "train"])
    ];

    public Task<Category?> GetByAlias(string alias, CancellationToken ct = default)
    {
        var category = categories.FirstOrDefault(c =>
            c.Aliases.Contains(alias, StringComparer.OrdinalIgnoreCase));

        return Task.FromResult(category);
    }

    public Task<IReadOnlyCollection<Category>> GetAll(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyCollection<Category>>(categories.AsReadOnly());
    }
}
