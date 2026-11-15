using Microsoft.Extensions.Logging;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Entities;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Logging;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Sync;

using static LogEvents.Sync;

internal partial class OutboxProcessor
{
    [LoggerMessage(
        EventId = ProcessingOutboxMessages,
        Level = LogLevel.Debug,
        Message = "Processing {Count} pending outbox messages")]
    partial void LogProcessingPendingMessages(int count);

    [LoggerMessage(
        EventId = OutboxMessageProcessed,
        Level = LogLevel.Debug,
        Message = "Successfully processed outbox message {Message}")]
    partial void LogMessageProcessed(OutboxMessageEntity message);

    [LoggerMessage(
        EventId = SyncCompleted,
        Level = LogLevel.Debug,
        Message = "Completed processing pending outbox messages")]
    partial void LogMessagesCompleted();

    [LoggerMessage(
        EventId = RetryScheduled,
        Level = LogLevel.Warning,
        Message = "Failed to process outbox message {Message}. Retry {RetryCount}/{MaxRetries} scheduled")]
    partial void LogRetryScheduled(Exception ex, OutboxMessageEntity message, int retryCount, int maxRetries);

    [LoggerMessage(
        EventId = MovedToDeadLetter,
        Level = LogLevel.Critical,
        Message = "Outbox message {Message} exceeded maximum retry attempts and was moved to Dead Letter")]
    partial void LogMovedToDeadLetter(Exception ex, OutboxMessageEntity message);
}
