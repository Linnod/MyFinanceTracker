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
        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult<Category?>(null);
        }

        var trimmed = query.Trim();
        var category = categories.FirstOrDefault(c =>
            c.Id.Equals(trimmed, StringComparison.OrdinalIgnoreCase) ||
            c.Name.Equals(trimmed, StringComparison.OrdinalIgnoreCase) ||
            c.Aliases.Any(a => a.Equals(trimmed, StringComparison.OrdinalIgnoreCase)));

        return Task.FromResult(category);
    }
}
