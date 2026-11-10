using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.Interactions.Telegram;

using static MyFinanceTracker.Interactions.Telegram.Logging.LogEvents.Notifications;

internal sealed partial class TelegramNotificationHandler
{
    [LoggerMessage(
        EventId = NotificationSendFailed,
        Level = LogLevel.Error,
        Message = "Failed to send Telegram notification")]
    partial void LogNotificationSendFailed(Exception ex);
}
