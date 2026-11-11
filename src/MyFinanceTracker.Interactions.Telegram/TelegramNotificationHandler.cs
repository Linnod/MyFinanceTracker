using MediatR;
using MyFinanceTracker.Common.Notifications;

namespace MyFinanceTracker.Interactions.Telegram;

internal sealed class TelegramNotificationHandler(ITelegramMessageSender messageSender) :
    INotificationHandler<OutboxSyncNotification.MessageMovedToDeadLetter>,
    INotificationHandler<OutboxSyncNotification.SyncFailed>
{
    public Task Handle(OutboxSyncNotification.MessageMovedToDeadLetter notification, CancellationToken ct) =>
        messageSender.Send(TelegramResponseFormatter.FormatNotification(notification), ct);

    public Task Handle(OutboxSyncNotification.SyncFailed notification, CancellationToken ct) =>
        messageSender.Send(TelegramResponseFormatter.FormatNotification(notification), ct);
}
