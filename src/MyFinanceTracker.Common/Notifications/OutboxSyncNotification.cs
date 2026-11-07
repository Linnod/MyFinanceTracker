using MediatR;

namespace MyFinanceTracker.Common.Notifications;

public abstract record OutboxSyncNotification : INotification
{
    private OutboxSyncNotification() { }

    public sealed record MessageMovedToDeadLetter(
        long OriginalOutboxMessageId,
        string OperationType,
        string ErrorMessage) : OutboxSyncNotification;

    public sealed record SyncFailed(
        string ErrorMessage) : OutboxSyncNotification;
}
