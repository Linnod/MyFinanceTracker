using Microsoft.Extensions.Logging;
using MyFinanceTracker.UseCases.Logging;

namespace MyFinanceTracker.UseCases.Transaction.GetById.Behaviors;

using static LogEvents.Transactions.GetById;

internal sealed partial class GetTransactionByIdLoggingBehavior
{
    [LoggerMessage(
        EventId = Starting,
        Level = LogLevel.Debug,
        Message = "Starting transaction retrieval by ID for request: {Request}")]
    partial void LogStarting(GetTransactionByIdRequest request);

    [LoggerMessage(
        EventId = Completed,
        Level = LogLevel.Information,
        Message = "Transaction retrieval by ID completed: {Response}")]
    partial void LogSuccess(GetTransactionByIdResponse.Success response);

    [LoggerMessage(
        EventId = ValidationFailed,
        Level = LogLevel.Information,
        Message = "Transaction retrieval by ID validation failed: {Error}")]
    partial void LogValidationError(GetTransactionByIdResponse.ValidationError error);

    [LoggerMessage(
        EventId = CriticalError,
        Level = LogLevel.Error,
        Message = "Critical failure during transaction retrieval by ID")]
    partial void LogCriticalError(Exception ex);
}
