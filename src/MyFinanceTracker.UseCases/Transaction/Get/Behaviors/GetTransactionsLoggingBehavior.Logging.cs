using Microsoft.Extensions.Logging;
using MyFinanceTracker.UseCases.Logging;

namespace MyFinanceTracker.UseCases.Transaction.Get.Behaviors;

using static LogEvents.Transactions.Get;

internal sealed partial class GetTransactionsLoggingBehavior
{
    [LoggerMessage(
        EventId = Starting,
        Level = LogLevel.Debug,
        Message = "Starting retrieval for request: {Request}")]
    partial void LogStarting(GetTransactionsRequest request);

    [LoggerMessage(
        EventId = Completed,
        Level = LogLevel.Information,
        Message = "Retrieval completed: {Response} (Time: {Ms}ms)")]
    partial void LogSuccess(GetTransactionsResponse.Success response, long ms);

    [LoggerMessage(
        EventId = ValidationFailed,
        Level = LogLevel.Information,
        Message = "Retrieval validation failed: {Error} (Time: {Ms}ms)")]
    partial void LogValidationError(GetTransactionsResponse.ValidationError error, long ms);

    [LoggerMessage(
        EventId = SystemError,
        Level = LogLevel.Warning,
        Message = "Retrieval finished with failure response (Time: {Ms}ms)")]
    partial void LogFailure(long ms);

    [LoggerMessage(
        EventId = SystemError,
        Level = LogLevel.Error,
        Message = "Critical failure during retrieval after {Ms}ms")]
    partial void LogCriticalError(Exception ex, long ms);
}