using Microsoft.Extensions.Logging;
using MyFinanceTracker.UseCases.Logging;

namespace MyFinanceTracker.UseCases.Category.Update.Behaviors;

using static LogEvents.Categories.Update;

internal sealed partial class UpdateCategoryLoggingBehavior
{
    [LoggerMessage(
        EventId = Starting,
        Level = LogLevel.Debug,
        Message = "Starting category update for request: {Request}")]
    partial void LogStarting(UpdateCategoryRequest request);

    [LoggerMessage(
        EventId = Completed,
        Level = LogLevel.Information,
        Message = "Category update completed: {Response}")]
    partial void LogSuccess(UpdateCategoryResponse.Success response);

    [LoggerMessage(
        EventId = ValidationFailed,
        Level = LogLevel.Information,
        Message = "Category update validation failed: {Error}")]
    partial void LogValidationError(UpdateCategoryResponse.ValidationError error);

    [LoggerMessage(
        EventId = CriticalError,
        Level = LogLevel.Error,
        Message = "Critical system failure during category update")]
    partial void LogCriticalError(Exception ex);
}
