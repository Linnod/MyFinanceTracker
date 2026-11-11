using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Configuration;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Loaders.Exceptions;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Parsers;

namespace MyFinanceTracker.Infrastructure.Persistence.Yaml.Loaders;

internal sealed partial class YamlCategoryLoader(
    IOptions<YamlPersistenceOptions> options,
    ICategoryParser parser,
    ILogger<YamlCategoryLoader> logger) : ICategoryLoader
{
    private readonly string resolvedPath = options.Value.FilePath;

    public IReadOnlyList<Category> Load()
    {
        if (!File.Exists(resolvedPath))
        {
            throw CategoryLoaderException.FileNotFound(resolvedPath);
        }

        try
        {
            var content = File.ReadAllText(resolvedPath);
            var categories = parser.Parse(content);

            LogLoaded(categories.Count, resolvedPath);

            return categories;
        }
        catch (Exception ex)
        {
            LogLoadFailed(resolvedPath, ex);
            throw;
        }
    }
}
