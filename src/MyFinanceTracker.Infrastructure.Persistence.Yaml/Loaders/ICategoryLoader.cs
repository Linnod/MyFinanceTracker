using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.Infrastructure.Persistence.Yaml.Loaders;

internal interface ICategoryLoader
{
    IReadOnlyList<Category> Load();
}
