using Microsoft.Extensions.Logging;
using MyFinanceTracker.UseCases.Logging;

namespace MyFinanceTracker.UseCases.Transaction.Create.Behaviors;

using static LogEvents.Transactions.Create;

internal sealed partial class CreateTransactionLoggingBehavior
{
    [LoggerMessage(
        EventId = Starting,
        Level = LogLevel.Debug,
        Message = "Starting transaction processing for request: {Request}")]
    partial void LogStarting(CreateTransactionRequest request);

    [LoggerMessage(
        EventId = Completed,
        Level = LogLevel.Debug,
        Message = "Transaction completed: {Response} (Time: {Ms}ms)")]
    partial void LogSuccess(CreateTransactionResponse.Success response, long ms);

    [LoggerMessage(
        EventId = ValidationFailed,
        Level = LogLevel.Information,
        Message = "Transaction validation failed: {Error} (Time: {Ms}ms)")]
    partial void LogValidationError(CreateTransactionResponse.ValidationError error, long ms);

    [LoggerMessage(
        EventId = SystemError,
        Level = LogLevel.Error,
        Message = "Critical system failure during processing after {Ms}ms")]
    partial void LogCriticalError(Exception ex, long ms);
}