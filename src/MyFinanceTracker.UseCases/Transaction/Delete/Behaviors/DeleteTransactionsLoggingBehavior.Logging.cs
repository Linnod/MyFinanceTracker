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
        Message = "Deletion completed: {Response}")]
    partial void LogSuccess(DeleteTransactionsResponse.Success response);

    [LoggerMessage(
        EventId = ValidationFailed,
        Level = LogLevel.Information,
        Message = "Deletion validation failed: {Error}")]
    partial void LogValidationError(DeleteTransactionsResponse.ValidationError error);

    [LoggerMessage(
        EventId = CriticalError,
        Level = LogLevel.Error,
        Message = "Critical failure during deletion")]
    partial void LogCriticalError(Exception ex);
}
