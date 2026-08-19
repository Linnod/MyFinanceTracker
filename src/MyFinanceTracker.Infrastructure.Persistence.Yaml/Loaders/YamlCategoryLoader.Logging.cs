using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.Infrastructure.Persistence.Yaml.Loaders;

using static Logging.LogEvents.Categories;

internal sealed partial class YamlCategoryLoader
{
    [LoggerMessage(
        EventId = Loading,
        Level = LogLevel.Debug,
        Message = "Loading categories from YAML file: '{FilePath}'")]
    private partial void LogLoading(string filePath);

    [LoggerMessage(
        EventId = Loaded,
        Level = LogLevel.Information,
        Message = "Successfully loaded {Count} categories with {AliasCount} aliases from '{FilePath}'")]
    private partial void LogLoaded(int count, int aliasCount, string filePath);

    [LoggerMessage(
        EventId = LoadError,
        Level = LogLevel.Error,
        Message = "Failed to load categories from '{FilePath}'")]
    private partial void LogLoadFailed(string filePath, Exception ex);
}
