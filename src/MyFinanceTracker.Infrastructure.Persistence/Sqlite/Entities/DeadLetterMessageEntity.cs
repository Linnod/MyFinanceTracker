namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Entities;

internal sealed class DeadLetterMessageEntity
{
    public long Id { get; set; }

    public long OriginalOutboxMessageId { get; set; }

    public OutboxOperationType OperationType { get; set; }

    public required string PayloadJson { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset FailedAtUtc { get; set; }

    public int RetryCount { get; set; }

    public required string ErrorMessage { get; set; }
}
