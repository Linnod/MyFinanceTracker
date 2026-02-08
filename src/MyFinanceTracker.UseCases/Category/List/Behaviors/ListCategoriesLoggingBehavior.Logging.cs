using Microsoft.Extensions.Logging;
using MyFinanceTracker.UseCases.Logging;

namespace MyFinanceTracker.UseCases.Category.List.Behaviors;

using static LogEvents.Categories.List;

internal sealed partial class ListCategoriesLoggingBehavior
{
    [LoggerMessage(
        EventId = Starting,
        Level = LogLevel.Debug,
        Message = "Fetching categories list. Request: {Request}")]
    partial void LogStarting(ListCategoriesRequest request);

    [LoggerMessage(
        EventId = Completed,
        Level = LogLevel.Information,
        Message = "Categories list retrieved. Count: {Count} (Time: {Ms}ms)")]
    partial void LogSuccess(int count, long ms);

    [LoggerMessage(
        EventId = SystemError,
        Level = LogLevel.Error,
        Message = "Failed to retrieve categories after {Ms}ms")]
    partial void LogCriticalError(Exception ex, long ms);
}