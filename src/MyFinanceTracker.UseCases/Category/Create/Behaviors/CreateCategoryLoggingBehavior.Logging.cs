using Microsoft.Extensions.Logging;
using MyFinanceTracker.UseCases.Logging;

namespace MyFinanceTracker.UseCases.Category.Create.Behaviors;

using static LogEvents.Categories.Create;

internal sealed partial class CreateCategoryLoggingBehavior
{
    [LoggerMessage(
        EventId = Starting,
        Level = LogLevel.Debug,
        Message = "Starting category creation for request: {Request}")]
    partial void LogStarting(CreateCategoryRequest request);

    [LoggerMessage(
        EventId = Completed,
        Level = LogLevel.Information,
        Message = "Category creation completed: {Response}")]
    partial void LogSuccess(CreateCategoryResponse.Success response);

    [LoggerMessage(
        EventId = ValidationFailed,
        Level = LogLevel.Information,
        Message = "Category creation validation failed: {Error}")]
    partial void LogValidationError(CreateCategoryResponse.ValidationError error);

    [LoggerMessage(
        EventId = CriticalError,
        Level = LogLevel.Error,
        Message = "Critical system failure during category creation")]
    partial void LogCriticalError(Exception ex);
}
