using Microsoft.Extensions.Logging;
using MyFinanceTracker.UseCases.Logging;

namespace MyFinanceTracker.UseCases.Transaction.Create.Behaviors;

using static LogEvents.Transactions.Create;

internal sealed partial class CreateTransactionsLoggingBehavior
{
    [LoggerMessage(
        EventId = Starting,
        Level = LogLevel.Debug,
        Message = "Starting transaction processing for request: {Request}")]
    partial void LogStarting(CreateTransactionsRequest request);

    [LoggerMessage(
        EventId = Completed,
        Level = LogLevel.Information,
        Message = "Transaction completed: {Response}")]
    partial void LogSuccess(CreateTransactionsResponse.Success response);

    [LoggerMessage(
        EventId = ValidationFailed,
        Level = LogLevel.Information,
        Message = "Transaction validation failed: {Error}")]
    partial void LogValidationError(CreateTransactionsResponse.ValidationError error);

    [LoggerMessage(
        EventId = CriticalError,
        Level = LogLevel.Error,
        Message = "Critical system failure during processing")]
    partial void LogCriticalError(Exception ex);
}
