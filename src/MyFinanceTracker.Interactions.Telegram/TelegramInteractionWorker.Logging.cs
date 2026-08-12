using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.Interactions.Telegram;

using static MyFinanceTracker.Interactions.Telegram.Logging.LogEvents.Worker;

internal sealed partial class TelegramInteractionWorker
{
    [LoggerMessage(
        EventId = Started,
        Level = LogLevel.Information,
        Message = "Telegram interaction worker started")]
    partial void LogStarted();

    [LoggerMessage(
        EventId = UpdateReceived,
        Level = LogLevel.Information,
        Message = "Processing message '{MessageText}'")]
    partial void LogUpdateReceived(string messageText);

    [LoggerMessage(
        EventId = UnauthorizedAccess,
        Level = LogLevel.Warning,
        Message = "Unauthorized access attempt for user ID {UserId}")]
    partial void LogUnauthorized(long? userId);

    [LoggerMessage(
        EventId = MessageSendFailed,
        Level = LogLevel.Error,
        Message = "Failed to send message for input '{Input}'")]
    partial void LogMessageSendFailed(Exception ex, string input);

    [LoggerMessage(
        EventId = PollingError,
        Level = LogLevel.Error,
        Message = "Telegram Bot API polling error")]
    partial void LogPollingError(Exception ex);
}