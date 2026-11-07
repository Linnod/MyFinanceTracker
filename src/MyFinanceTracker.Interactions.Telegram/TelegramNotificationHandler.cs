using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyFinanceTracker.Common.Notifications;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace MyFinanceTracker.Interactions.Telegram;

internal sealed partial class TelegramNotificationHandler(
    ITelegramBotClient botClient,
    IOptions<TelegramInteractionOptions> options,
    ILogger<TelegramNotificationHandler> logger) :
    INotificationHandler<OutboxSyncNotification.MessageMovedToDeadLetter>,
    INotificationHandler<OutboxSyncNotification.SyncFailed>
{
    public Task Handle(OutboxSyncNotification.MessageMovedToDeadLetter notification, CancellationToken ct) =>
        SendNotification(notification, ct);

    public Task Handle(OutboxSyncNotification.SyncFailed notification, CancellationToken ct) =>
        SendNotification(notification, ct);

    private async Task SendNotification(OutboxSyncNotification notification, CancellationToken ct)
    {
        var text = TelegramResponseFormatter.FormatNotification(notification);

        try
        {
            await botClient.SendMessage(
                chatId: options.Value.AllowedUserId,
                text: text,
                parseMode: ParseMode.Html,
                cancellationToken: ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogNotificationSendFailed(ex, options.Value.AllowedUserId);
        }
    }
}
