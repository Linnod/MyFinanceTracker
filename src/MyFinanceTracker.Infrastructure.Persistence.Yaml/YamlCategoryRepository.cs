using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Loaders;

namespace MyFinanceTracker.Infrastructure.Persistence.Yaml;

internal sealed class YamlCategoryRepository(ICategoryLoader loader) : ICategoryRepository
{
    private readonly IReadOnlyList<Category> categories = loader.Load();

    public Task<IReadOnlyList<Category>> GetAll(CancellationToken ct)
        => Task.FromResult(categories);
}