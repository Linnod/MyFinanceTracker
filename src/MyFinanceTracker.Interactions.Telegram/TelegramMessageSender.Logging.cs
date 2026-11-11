using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.Interactions.Telegram;

using static MyFinanceTracker.Interactions.Telegram.Logging.LogEvents.Sender;

internal sealed partial class TelegramMessageSender
{
    [LoggerMessage(
        EventId = SendFailed,
        Level = LogLevel.Error,
        Message = "Failed to send Telegram message to allowed user")]
    partial void LogSendFailed(Exception ex);
}
