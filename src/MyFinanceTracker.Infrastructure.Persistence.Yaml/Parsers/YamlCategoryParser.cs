using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Exceptions;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Parsers.Exceptions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MyFinanceTracker.Infrastructure.Persistence.Yaml.Parsers;

internal sealed class YamlCategoryParser : ICategoryParser
{
    public IReadOnlyList<Category> Parse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw CategoryParserException.EmptyContent();
        }

        var yamlData = Deserialize(content);
        return ValidateAndMap(yamlData);
    }

    private static YamlCategoryRoot? Deserialize(string content)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        try
        {
            return deserializer.Deserialize<YamlCategoryRoot>(content);
        }
        catch (Exception ex)
        {
            throw CategoryParserException.DeserializationFailed(ex);
        }
    }

    private static IReadOnlyList<Category> ValidateAndMap(YamlCategoryRoot? data)
    {
        if (data?.Categories == null || data.Categories.Count == 0)
        {
            throw CategoryParserException.NoCategoriesFound();
        }

        var invalidCategory = data.Categories
            .FirstOrDefault(c => c.Aliases == null || c.Aliases.Count == 0);

        if (invalidCategory != null)
        {
            throw CategoryParserException.MissingAliases(invalidCategory.Id);
        }

        var duplicateId = data.Categories
            .GroupBy(c => c.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateId != null)
        {
            throw CategoryParserException.DuplicateId(duplicateId.Key);
        }

        var duplicateName = data.Categories
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateName != null)
        {
            throw CategoryParserException.DuplicateName(duplicateName.Key);
        }

        var allAliases = data.Categories
            .SelectMany(c => c.Aliases)
            .ToList();

        var duplicateAlias = allAliases
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateAlias != null)
        {
            throw CategoryParserException.DuplicateAlias(duplicateAlias.Key);
        }

        try
        {
            return [.. data.Categories
                .Select(c => new Category(c.Id, c.Name, c.Aliases, c.Archived))];
        }
        catch (DomainException ex)
        {
            throw CategoryParserException.InvalidCategory(ex);
        }
    }

    private sealed class YamlCategoryRoot
    {
        public List<YamlCategoryItem> Categories { get; set; } = [];
    }

    private sealed class YamlCategoryItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<string> Aliases { get; set; } = [];
        public bool Archived { get; set; }
    }
}
