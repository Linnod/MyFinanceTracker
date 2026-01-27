using Microsoft.Extensions.Options;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Configuration;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Loaders.Exceptions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MyFinanceTracker.Infrastructure.Persistence.Yaml.Loaders;

internal class YamlCategoryLoader(
    IOptions<YamlPersistenceOptions> options) : ICategoryLoader
{
    private readonly string resolvedPath = Path.Combine(AppContext.BaseDirectory, options.Value.FilePath);

    public List<Category> Load()
    {
        if (!File.Exists(resolvedPath))
        {
            throw CategoryLoaderException.FileNotFound(resolvedPath);
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        try
        {
            using var reader = new StreamReader(resolvedPath);
            var yamlData = deserializer.Deserialize<YamlCategoryRoot>(reader);

            var result = ValidateAndMap(yamlData);
            
            return result;
        }
        catch (CategoryLoaderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw CategoryLoaderException.DeserializationFailed(resolvedPath, ex);
        }
    }

    private static List<Category> ValidateAndMap(YamlCategoryRoot? data)
    {
        if (data?.Categories == null || data.Categories.Count == 0)
        {
            return [];
        }

        var duplicateId = data.Categories
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateId != null)
        {
            throw CategoryLoaderException.DuplicateId(duplicateId.Key);
        }

        var allAliasesWithIds = data.Categories
            .SelectMany(c => c.Aliases.Append(c.Id))
            .ToList();

        var duplicateAlias = allAliasesWithIds
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateAlias != null)
        {
            throw CategoryLoaderException.DuplicateAlias(duplicateAlias.Key);
        }

        var hasDefaultIncome = allAliasesWithIds
            .Contains(FinancialRules.DefaultIncomeCategoryAlias, StringComparer.OrdinalIgnoreCase);

        if (!hasDefaultIncome)
        {
            throw CategoryLoaderException.DefaultIncomeCategoryMissing(FinancialRules.DefaultIncomeCategoryAlias);
        }

        return [.. data.Categories
            .Select(c => new Category(c.Id, c.Name, c.Aliases, c.IsIncome))];
    }

    private sealed class YamlCategoryRoot
    {
        public List<YamlCategoryItem> Categories { get; set; } = [];
    }

    private sealed class YamlCategoryItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsIncome { get; set; }
        public List<string> Aliases { get; set; } = [];
    }
}