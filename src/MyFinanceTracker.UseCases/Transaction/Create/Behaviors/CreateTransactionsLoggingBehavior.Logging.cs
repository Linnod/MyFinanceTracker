using Microsoft.Extensions.Logging;
using MyFinanceTracker.Domain.Entities;
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
        Message = "Transaction completed: {Response} (Time: {Ms}ms)")]
    partial void LogSuccess(CreateTransactionsResponse.Success response, long ms);

    [LoggerMessage(
        EventId = ValidationFailed,
        Level = LogLevel.Information,
        Message = "Transaction validation failed: {Error} (Time: {Ms}ms)")]
    partial void LogValidationError(CreateTransactionsResponse.ValidationError error, long ms);

    [LoggerMessage(
        EventId = CategoryNotFound,
        Level = LogLevel.Information,
        Message = "Category '{RequestedAlias}' not found. (Time: {Ms}ms)")]
    partial void LogCategoryNotFound(string requestedAlias, long ms);

    [LoggerMessage(
        EventId = CategoryRequired,
        Level = LogLevel.Information,
        Message = "Category is required for transaction type '{Type}' (Time: {Ms}ms)")]
    partial void LogCategoryRequired(TransactionType type, long ms);

    [LoggerMessage(
        EventId = SystemError,
        Level = LogLevel.Warning,
        Message = "Transaction processing finished with failure response (Time: {Ms}ms)")]
    partial void LogFailure(long ms);

    [LoggerMessage(
        EventId = SystemError,
        Level = LogLevel.Error,
        Message = "Critical system failure during processing after {Ms}ms")]
    partial void LogCriticalError(Exception ex, long ms);
}