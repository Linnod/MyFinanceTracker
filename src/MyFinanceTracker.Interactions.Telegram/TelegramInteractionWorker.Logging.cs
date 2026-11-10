using Microsoft.Extensions.Logging;
using MyFinanceTracker.InputProcessing;
using Telegram.Bot.Types.Enums;

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
        EventId = IncomingMessageReceived,
        Level = LogLevel.Information,
        Message = "Received Telegram message (Type: {MessageType}, MessageId: {MessageId})")]
    partial void LogIncomingMessage(MessageType messageType, int messageId);

    [LoggerMessage(
        EventId = UpdateReceived,
        Level = LogLevel.Information,
        Message = "Processing message '{Input}'")]
    partial void LogUpdateReceived(Input input);

    [LoggerMessage(
        EventId = UnauthorizedAccess,
        Level = LogLevel.Warning,
        Message = "Unauthorized access attempt for user ID {UserId}")]
    partial void LogUnauthorized(long? userId);

    [LoggerMessage(
        EventId = UpdateProcessingFailed,
        Level = LogLevel.Error,
        Message = "Failed to process message {MessageId}")]
    partial void LogUpdateProcessingFailed(Exception ex, int messageId);

    [LoggerMessage(
        EventId = MessageSendFailed,
        Level = LogLevel.Error,
        Message = "Failed to send response to message {MessageId}")]
    partial void LogMessageSendFailed(Exception ex, int messageId);

    [LoggerMessage(
        EventId = PollingError,
        Level = LogLevel.Error,
        Message = "Telegram Bot API polling error")]
    partial void LogPollingError(Exception ex);
}
