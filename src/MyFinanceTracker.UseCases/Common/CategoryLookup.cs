using MyFinanceTracker.Common.Utilities;
using DomainCategory = MyFinanceTracker.Domain.Entities.Category;

namespace MyFinanceTracker.UseCases.Common;

internal sealed class CategoryLookup(IEnumerable<DomainCategory> categories)
{
    private readonly Dictionary<string, DomainCategory> map = categories
            .SelectMany(c => c.Aliases.Select(alias => (Alias: alias, Category: c)))
            .ToDictionary(x => x.Alias, x => x.Category, StringComparer.OrdinalIgnoreCase);

    public CategoryResolution Resolve(string alias)
    {
        if (map.TryGetValue(alias, out var category))
        {
            return new CategoryResolution.Found(category);
        }

        var suggestion = FuzzyMatcher.GetClosest(alias, map.Keys);
        return new CategoryResolution.NotFound(suggestion);
    }
}
