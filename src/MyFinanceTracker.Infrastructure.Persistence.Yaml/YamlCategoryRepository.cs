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

    public Task<IReadOnlyCollection<string>> GetAllAliases(CancellationToken ct = default)
    {
        var allAliases = categories.Value
            .SelectMany(c => c.Aliases)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<string>>(allAliases);
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