using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.Interactions.Telegram;

using static MyFinanceTracker.Interactions.Telegram.Logging.LogEvents.Worker;

internal sealed partial class TelegramInteractionWorker
{
    private const string LogPrefix = "[" + nameof(TelegramInteractionWorker) + "]";

    [LoggerMessage(
        EventId = Started,
        Level = LogLevel.Information,
        Message = "--> " + LogPrefix + " Telegram interaction worker started")]
    partial void LogStarted();

    [LoggerMessage(
        EventId = UpdateReceived,
        Level = LogLevel.Information,
        Message = "--> " + LogPrefix + " Processing message: '{MessageText}'")]
    partial void LogUpdateReceived(string messageText);

    [LoggerMessage(
        EventId = UnauthorizedAccess,
        Level = LogLevel.Warning,
        Message = "!! " + LogPrefix + " Unauthorized access attempt. UserID: {UserId}")]
    partial void LogUnauthorized(long? userId);

    [LoggerMessage(
        EventId = MessageSendFailed,
        Level = LogLevel.Error,
        Message = "!! " + LogPrefix + " Failed to send message. Input: {Input}")]
    partial void LogMessageSendFailed(Exception ex, string input);

    [LoggerMessage(
        EventId = PollingError,
        Level = LogLevel.Error,
        Message = "!! " + LogPrefix + " Telegram Bot API polling error")]
    partial void LogPollingError(Exception ex);
}