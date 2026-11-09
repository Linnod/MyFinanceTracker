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
        Level = LogLevel.Debug,
        Message = "Categories list retrieved. Count: {Count}")]
    partial void LogSuccess(int count);

    [LoggerMessage(
        EventId = CriticalError,
        Level = LogLevel.Error,
        Message = "Failed to retrieve categories")]
    partial void LogCriticalError(Exception ex);
}
