using Microsoft.Extensions.Logging;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Loaders;

namespace MyFinanceTracker.Infrastructure.Persistence.Yaml;

internal class YamlCategoryRepository(
    ICategoryLoader loader,
    ILogger<YamlCategoryRepository> logger) : ICategoryRepository
{
    private readonly Lazy<List<Category>> categories = new(loader.Load, LazyThreadSafetyMode.ExecutionAndPublication);

    public Task<IReadOnlyCollection<Category>> GetAll(CancellationToken ct = default)
    {
        logger.LogInformation("--> GetAll");

        var result = Task.FromResult<IReadOnlyCollection<Category>>([.. categories.Value]);

        logger.LogInformation("<-- GetAll");
        
        return result;
    }

    public Task<Category?> GetByAlias(string alias, CancellationToken ct = default)
    {
        logger.LogInformation("--> GetByAlias");

        var category = categories.Value.FirstOrDefault(c =>
        {
            return c.Aliases.Contains(alias, StringComparer.OrdinalIgnoreCase);
        });

        logger.LogInformation("<-- GetByAlias");

        return Task.FromResult(category);
    }
}