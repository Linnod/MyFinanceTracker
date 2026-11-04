using DomainCategory = MyFinanceTracker.Domain.Entities.Category;

namespace MyFinanceTracker.UseCases.Common;

internal sealed class CategoryLookup
{
    private readonly Dictionary<string, DomainCategory> map = new(StringComparer.OrdinalIgnoreCase);

    public CategoryLookup(IEnumerable<DomainCategory> categories)
    {
        foreach (var category in categories)
        {
            map.TryAdd(category.Id, category);
            map.TryAdd(category.Name, category);

            foreach (var alias in category.Aliases)
            {
                map.TryAdd(alias, category);
            }
        }
    }

    public DomainCategory? Resolve(string alias)
    {
        return map.TryGetValue(alias, out var category) ? category : null;
    }
}
