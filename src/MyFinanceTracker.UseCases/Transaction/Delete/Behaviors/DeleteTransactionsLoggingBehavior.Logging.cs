using Microsoft.Extensions.Logging;
using MyFinanceTracker.UseCases.Logging;

namespace MyFinanceTracker.UseCases.Transaction.Delete.Behaviors;

using static LogEvents.Transactions.Delete;

internal sealed partial class DeleteTransactionsLoggingBehavior
{
    [LoggerMessage(
        EventId = Starting,
        Level = LogLevel.Debug,
        Message = "Starting deletion for request: {Request}")]
    partial void LogStarting(DeleteTransactionsRequest request);

    [LoggerMessage(
        EventId = Completed,
        Level = LogLevel.Information,
        Message = "Deletion completed: {Response} (Time: {Ms}ms)")]
    partial void LogSuccess(DeleteTransactionsResponse.Success response, long ms);

    [LoggerMessage(
        EventId = ValidationFailed,
        Level = LogLevel.Information,
        Message = "Deletion validation failed: {Error} (Time: {Ms}ms)")]
    partial void LogValidationError(DeleteTransactionsResponse.ValidationError error, long ms);

    [LoggerMessage(
        EventId = CategoryNotFound,
        Level = LogLevel.Information,
        Message = "Deletion failed. Category '{RequestedAlias}' not found (Time: {Ms}ms)")]
    partial void LogCategoryNotFound(string requestedAlias, long ms);

    [LoggerMessage(
        EventId = SystemError,
        Level = LogLevel.Warning,
        Message = "Deletion finished with failure response (Time: {Ms}ms)")]
    partial void LogFailure(long ms);

    [LoggerMessage(
        EventId = SystemError,
        Level = LogLevel.Error,
        Message = "Critical failure during deletion after {Ms}ms")]
    partial void LogCriticalError(Exception ex, long ms);
}