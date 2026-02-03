using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.Infrastructure.Persistence.Yaml.Loaders;

internal interface ICategoryLoader
{
    IReadOnlyCollection<Category> Load();
}
