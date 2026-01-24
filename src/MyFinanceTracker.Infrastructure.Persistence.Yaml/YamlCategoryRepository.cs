using Microsoft.Extensions.Options;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Configuration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MyFinanceTracker.Infrastructure.Persistence.Yaml;

internal class YamlCategoryRepository : ICategoryRepository
{
    private readonly string resolvedPath;
    private readonly Lazy<List<Category>> categories;

    public YamlCategoryRepository(IOptions<YamlPersistenceOptions> options)
    {
        resolvedPath = Path.Combine(AppContext.BaseDirectory, options.Value.FilePath);
        categories = new Lazy<List<Category>>(LoadCategories, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public Task<IReadOnlyCollection<Category>> GetAll(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyCollection<Category>>(categories.Value.AsReadOnly());
    }

    public Task<Category?> GetByAlias(string alias, CancellationToken ct = default)
    {
        var category = categories.Value.FirstOrDefault(c =>
            c.Aliases.Contains(alias, StringComparer.OrdinalIgnoreCase));

        return Task.FromResult(category);
    }

    private List<Category> LoadCategories()
    {
        if (!File.Exists(resolvedPath))
        {
            throw new FileNotFoundException($"Critical error: YAML category file not found at: {resolvedPath}", resolvedPath);
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        try
        {
            using var reader = new StreamReader(resolvedPath);
            var yamlData = deserializer.Deserialize<YamlCategoryRoot>(reader);

            if (yamlData?.Categories == null)
            {
                return [];
            }

            return yamlData.Categories
                .Select(c => new Category(c.Id, c.Name, c.Aliases, c.IsIncome))
                .ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to deserialize YAML category file at: {resolvedPath}", ex);
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
        public bool IsIncome { get; set; }
        public List<string> Aliases { get; set; } = [];
    }
}