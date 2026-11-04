using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Loaders;

namespace MyFinanceTracker.Infrastructure.Persistence.Yaml;

internal sealed class YamlCategoryRepository(ICategoryLoader loader) : ICategoryRepository
{
    private readonly IReadOnlyList<Category> categories = loader.Load();

    public Task<IReadOnlyList<Category>> GetAll(CancellationToken ct)
        => Task.FromResult(categories);

    public Task<Category?> Resolve(string query, CancellationToken ct)
    {
        var category = categories.FirstOrDefault(c =>
            c.Id.Equals(query, StringComparison.OrdinalIgnoreCase) ||
            c.Name.Equals(query, StringComparison.OrdinalIgnoreCase) ||
            c.Aliases.Any(a => a.Equals(query, StringComparison.OrdinalIgnoreCase)));

        return Task.FromResult(category);
    }
}
