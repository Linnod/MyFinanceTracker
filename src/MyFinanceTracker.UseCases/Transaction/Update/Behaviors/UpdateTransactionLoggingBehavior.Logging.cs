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
        Message = "Transaction update completed: {Response}")]
    partial void LogSuccess(UpdateTransactionResponse.Success response);

    [LoggerMessage(
        EventId = ValidationFailed,
        Level = LogLevel.Information,
        Message = "Transaction update validation failed: {Error}")]
    partial void LogValidationError(UpdateTransactionResponse.ValidationError error);

    [LoggerMessage(
        EventId = CriticalError,
        Level = LogLevel.Error,
        Message = "Critical system failure during transaction update")]
    partial void LogCriticalError(Exception ex);
}
