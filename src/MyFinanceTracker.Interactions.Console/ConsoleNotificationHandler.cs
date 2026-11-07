using MediatR;
using MyFinanceTracker.Common.Notifications;

namespace MyFinanceTracker.Interactions.Console;

internal sealed class ConsoleNotificationHandler :
    INotificationHandler<OutboxSyncNotification.MessageMovedToDeadLetter>,
    INotificationHandler<OutboxSyncNotification.SyncFailed>
{
    public Task Handle(OutboxSyncNotification.MessageMovedToDeadLetter notification, CancellationToken ct)
    {
        ConsoleCommands.WriteError($"Outbox message #{notification.OriginalOutboxMessageId} ({notification.OperationType}) moved to Dead Letter: {notification.ErrorMessage}");
        return Task.CompletedTask;
    }

    public Task Handle(OutboxSyncNotification.SyncFailed notification, CancellationToken ct)
    {
        ConsoleCommands.WriteError($"Outbox background sync failed: {notification.ErrorMessage}");
        return Task.CompletedTask;
    }
}
