namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Entities;

internal sealed class OutboxMessageEntity
{
    public long Id { get; set; }

    public OutboxOperationType OperationType { get; set; }

    public required string PayloadJson { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public int RetryCount { get; set; }

    public override string ToString() =>
        $"[Id: {Id}, Type: {OperationType}, Retries: {RetryCount}, Payload: {PayloadJson}]";
}
