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
        Message = "Retrieval completed: {Response}")]
    partial void LogSuccess(GetTransactionsResponse.Success response);

    [LoggerMessage(
        EventId = ValidationFailed,
        Level = LogLevel.Information,
        Message = "Retrieval validation failed: {Error}")]
    partial void LogValidationError(GetTransactionsResponse.ValidationError error);

    [LoggerMessage(
        EventId = CriticalError,
        Level = LogLevel.Error,
        Message = "Critical failure during retrieval")]
    partial void LogCriticalError(Exception ex);
}
