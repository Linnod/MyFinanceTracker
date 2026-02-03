using Microsoft.Extensions.Logging;
using MyFinanceTracker.UseCases.Logging;

namespace MyFinanceTracker.UseCases.Transaction.Delete.Behaviors;

internal sealed partial class DeleteTransactionsLoggingBehavior
{
    [LoggerMessage(
        EventId = LogEvents.Transactions.Delete.Starting,
        Level = LogLevel.Debug,
        Message = "Starting deletion for request: {Request}")]
    partial void LogStarting(DeleteTransactionsRequest request);

    [LoggerMessage(
        EventId = LogEvents.Transactions.Delete.Completed,
        Level = LogLevel.Debug,
        Message = "Deletion completed: {Response} (Time: {Ms}ms)")]
    partial void LogSuccess(DeleteTransactionsResponse.Success response, long ms);

    [LoggerMessage(
        EventId = LogEvents.Transactions.Delete.ValidationFailed,
        Level = LogLevel.Information,
        Message = "Deletion validation failed: {Error} (Time: {Ms}ms)")]
    partial void LogValidationError(DeleteTransactionsResponse.ValidationError error, long ms);

    [LoggerMessage(
        EventId = LogEvents.Transactions.Delete.SystemError,
        Level = LogLevel.Error,
        Message = "Critical failure during deletion after {Ms}ms")]
    partial void LogCriticalError(Exception ex, long ms);
}
