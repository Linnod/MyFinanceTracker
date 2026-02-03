using Microsoft.Extensions.Logging;
using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.Infrastructure.Persistence.Yaml;

using static MyFinanceTracker.Infrastructure.Persistence.Yaml.Logging.LogEvents.Categories;

internal sealed partial class YamlCategoryRepository
{
    [LoggerMessage(
        EventId = Searching,
        Level = LogLevel.Debug,
        Message = "Searching for category by alias '{Alias}'")]
    partial void LogSearching(string alias);

    [LoggerMessage(
        EventId = Found,
        Level = LogLevel.Debug,
        Message = "Category found: '{Category}'")]
    partial void LogFound(Category category);

    [LoggerMessage(
        EventId = NotFound,
        Level = LogLevel.Information,
        Message = "Category NOT found for alias '{Alias}'")]
    partial void LogNotFound(string alias);
}