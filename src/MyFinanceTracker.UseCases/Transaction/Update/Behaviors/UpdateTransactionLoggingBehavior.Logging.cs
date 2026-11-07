using Microsoft.Extensions.Logging;
using MyFinanceTracker.UseCases.Logging;

namespace MyFinanceTracker.UseCases.Transaction.Update.Behaviors;

using static LogEvents.Transactions.Update;

internal sealed partial class UpdateTransactionLoggingBehavior
{
    [LoggerMessage(
        EventId = Starting,
        Level = LogLevel.Debug,
        Message = "Starting transaction update for request: {Request}")]
    partial void LogStarting(UpdateTransactionRequest request);

    [LoggerMessage(
        EventId = Completed,
        Level = LogLevel.Information,
        Message = "Transaction update completed: {Response} (Time: {Ms}ms)")]
    partial void LogSuccess(UpdateTransactionResponse.Success response, long ms);

    [LoggerMessage(
        EventId = ValidationFailed,
        Level = LogLevel.Information,
        Message = "Transaction update validation failed: {Error} (Time: {Ms}ms)")]
    partial void LogValidationError(UpdateTransactionResponse.ValidationError error, long ms);

    [LoggerMessage(
        EventId = Failure,
        Level = LogLevel.Warning,
        Message = "Transaction update finished with failure response (Time: {Ms}ms)")]
    partial void LogFailure(long ms);

    [LoggerMessage(
        EventId = CriticalError,
        Level = LogLevel.Error,
        Message = "Critical system failure during transaction update after {Ms}ms")]
    partial void LogCriticalError(Exception ex, long ms);
}
