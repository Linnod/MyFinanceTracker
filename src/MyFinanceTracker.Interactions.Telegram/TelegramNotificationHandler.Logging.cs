using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.Interactions.Telegram;

using static MyFinanceTracker.Interactions.Telegram.Logging.LogEvents.Notifications;

internal sealed partial class TelegramNotificationHandler
{
    [LoggerMessage(
        EventId = NotificationSendFailed,
        Level = LogLevel.Error,
        Message = "Failed to send notification to user {UserId}")]
    partial void LogNotificationSendFailed(Exception ex, long userId);
}
