using Microsoft.Extensions.Options;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Configuration;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Loaders.Exceptions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MyFinanceTracker.Infrastructure.Persistence.Yaml.Loaders;

internal class YamlCategoryLoader(IOptions<YamlPersistenceOptions> options) : ICategoryLoader
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

            return ValidateAndMap(yamlData);
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
            .FirstOrDefault(g =>
            {
                return g.Count() > 1;
            });

        if (duplicateId != null)
        {
            throw CategoryLoaderException.DuplicateId(duplicateId.Key);
        }

        var allAliases = data.Categories.SelectMany(c =>
        {
            return c.Aliases;
        }).ToList();

        var duplicateAlias = allAliases
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g =>
            {
                return g.Count() > 1;
            });

        if (duplicateAlias != null)
        {
            throw CategoryLoaderException.DuplicateAlias(duplicateAlias.Key);
        }

        return [.. data.Categories
            .Select(c =>
            {
                return new Category(c.Id, c.Name, c.Aliases, c.IsIncome);
            })];
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