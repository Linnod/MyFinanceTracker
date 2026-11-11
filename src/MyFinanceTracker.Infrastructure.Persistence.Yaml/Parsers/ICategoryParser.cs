using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.Infrastructure.Persistence.Yaml.Parsers;

internal interface ICategoryParser
{
    IReadOnlyList<Category> Parse(string content);
}
