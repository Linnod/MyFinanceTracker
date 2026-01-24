using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Loaders;

namespace MyFinanceTracker.Infrastructure.Persistence.Yaml;

internal class YamlCategoryRepository(ICategoryLoader loader) : ICategoryRepository
{
    private readonly Lazy<List<Category>> categories = new(loader.Load, LazyThreadSafetyMode.ExecutionAndPublication);

    public Task<IReadOnlyCollection<Category>> GetAll(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyCollection<Category>>([.. categories.Value]);
    }

    public Task<Category?> GetByAlias(string alias, CancellationToken ct = default)
    {
        var category = categories.Value.FirstOrDefault(c =>
        {
            return c.Aliases.Contains(alias, StringComparer.OrdinalIgnoreCase);
        });

        return Task.FromResult(category);
    }
}