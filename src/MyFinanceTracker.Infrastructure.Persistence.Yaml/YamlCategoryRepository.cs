using Microsoft.Extensions.Logging;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Loaders;

namespace MyFinanceTracker.Infrastructure.Persistence.Yaml;

internal sealed partial class YamlCategoryRepository(
    ICategoryLoader loader,
    ILogger<YamlCategoryRepository> logger) : ICategoryRepository
{
    private readonly Lazy<IReadOnlyCollection<Category>> categories = new(loader.Load, LazyThreadSafetyMode.ExecutionAndPublication);

    public Task<Category?> GetByAlias(string alias, CancellationToken ct = default)
    {
        LogSearching(alias);

        var category = categories.Value.FirstOrDefault(c => c.Aliases.Contains(alias, StringComparer.OrdinalIgnoreCase));

        LogSearchResult(alias, category);

        return Task.FromResult(category);
    }

    public Task<IReadOnlyCollection<Category>> GetAll(CancellationToken ct = default)
    {
        return Task.FromResult(categories.Value);
    }

    private void LogSearchResult(string alias, Category? category)
    {
        if (category != null)
        {
            LogFound(category);
        }
        else
        {
            LogNotFound(alias);
        }
    }
}