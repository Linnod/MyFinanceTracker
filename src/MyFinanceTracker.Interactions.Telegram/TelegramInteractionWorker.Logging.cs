using Microsoft.Extensions.Logging;
using MyFinanceTracker.InputProcessing;

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
        Message = "Processing message '{Input}'")]
    partial void LogUpdateReceived(IInput input);

    [LoggerMessage(
        EventId = UnauthorizedAccess,
        Level = LogLevel.Warning,
        Message = "Unauthorized access attempt for user ID {UserId}")]
    partial void LogUnauthorized(long? userId);

    [LoggerMessage(
        EventId = InputBuildFailed,
        Level = LogLevel.Error,
        Message = "Failed to prepare input from Telegram message")]
    partial void LogInputBuildFailed(Exception ex);

    [LoggerMessage(
        EventId = MessageSendFailed,
        Level = LogLevel.Error,
        Message = "Failed to send response to message {MessageId} in chat {ChatId}")]
    partial void LogMessageSendFailed(Exception ex, long chatId, int messageId);

    [LoggerMessage(
        EventId = PollingError,
        Level = LogLevel.Error,
        Message = "Telegram Bot API polling error")]
    partial void LogPollingError(Exception ex);
}
