using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Entities;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Models;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Sync.Exceptions;

internal sealed class OutboxProcessingException : Exception
{
    public long MessageId { get; }

    public OutboxOperationType OperationType { get; }

    private OutboxProcessingException(
        long messageId,
        OutboxOperationType operationType,
        string message,
        Exception? innerException = null)
        : base($"Failed to process outbox message {messageId} of type '{operationType}': {message}", innerException)
    {
        MessageId = messageId;
        OperationType = operationType;
    }

    public static OutboxProcessingException CategoryNotFound(
        OutboxMessageEntity message,
        TransactionOutboxDto dto) =>
        new(message.Id, message.OperationType, $"Category '{dto.CategoryId}' for transaction '{dto.Id}' could not be resolved.");

    public static OutboxProcessingException DeserializationFailed(
        OutboxMessageEntity message,
        Exception? inner = null) =>
        new(message.Id, message.OperationType, "Failed to deserialize message payload.", inner);

    public static OutboxProcessingException UnsupportedOperation(
        OutboxMessageEntity message) =>
        new(message.Id, message.OperationType, $"Unsupported outbox operation type '{message.OperationType}'.");
}
