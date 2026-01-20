using Microsoft.Extensions.Options;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MyFinanceTracker.Infrastructure.Persistence.Yaml;

internal class YamlCategoryRepository : ICategoryRepository
{
    private readonly YamlPersistenceOptions options;
    private readonly Lazy<List<Category>> categories;

    public YamlCategoryRepository(IOptions<YamlPersistenceOptions> options)
    {
        this.options = options.Value;
        categories = new Lazy<List<Category>>(() => LoadCategories(), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public Task<IReadOnlyCollection<Category>> GetAll(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyCollection<Category>>(categories.Value.AsReadOnly());
    }

    public Task<Category?> GetByAlias(string alias, CancellationToken ct = default)
    {
        return Task.FromResult(categories.Value.FirstOrDefault(c =>
                    c.Aliases.Contains(alias, StringComparer.OrdinalIgnoreCase)));
    }

    private List<Category> LoadCategories()
    {
        if (!File.Exists(options.FilePath))
        {
            throw new FileNotFoundException(options.FilePath);
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        using var reader = new StreamReader(options.FilePath);
        var yamlData = deserializer.Deserialize<YamlCategoryRoot>(reader);

        return [.. yamlData.Categories.Select(c => new Category(c.Id, c.Name, c.Aliases, c.IsIncome))];
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
