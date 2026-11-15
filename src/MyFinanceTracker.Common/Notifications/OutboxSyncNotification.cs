using MediatR;

namespace MyFinanceTracker.Common.Notifications;

public closed record OutboxSyncNotification : INotification
{
    public sealed record MessageMovedToDeadLetter(
        long OriginalOutboxMessageId,
        string OperationType,
        string ErrorMessage) : OutboxSyncNotification;

    public sealed record SyncFailed(
        string ErrorMessage) : OutboxSyncNotification;
}
