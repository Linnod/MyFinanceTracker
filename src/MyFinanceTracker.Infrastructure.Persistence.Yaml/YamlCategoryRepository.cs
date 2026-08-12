using Microsoft.Extensions.Logging;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Loaders;

namespace MyFinanceTracker.Infrastructure.Persistence.Yaml;

internal sealed partial class YamlCategoryRepository : ICategoryRepository
{
    private readonly IReadOnlyList<Category> categories;
    private readonly Dictionary<string, Category> aliasMap;
    private readonly ILogger<YamlCategoryRepository> logger;

    public YamlCategoryRepository(
        ICategoryLoader loader, 
        ILogger<YamlCategoryRepository> logger)
    {
        this.logger = logger;
        categories = loader.Load();
        
        aliasMap = categories
            .SelectMany(cat => cat.Aliases.Select(alias => (Alias: alias, Category: cat)))
            .ToDictionary(x => x.Alias, x => x.Category, StringComparer.OrdinalIgnoreCase);
    }

    public Task<Category?> GetByAlias(string alias, CancellationToken ct)
    {
        LogSearching(alias);

        aliasMap.TryGetValue(alias, out var category);

        LogSearchResult(alias, category);

        return Task.FromResult(category);
    }

    public Task<IReadOnlyList<Category>> GetAll(CancellationToken ct) 
        => Task.FromResult(categories);

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