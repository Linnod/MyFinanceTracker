using Microsoft.Extensions.Logging;
using MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands;

namespace MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching;

using static MyFinanceTracker.CommandProcessing.Text.Logging.LogEvents.Dispatcher;

internal sealed partial class TextCommandDispatcher
{
    [LoggerMessage(
        EventId = Entry,
        Level = LogLevel.Debug,
        Message = "--> Dispatching command '{Type}' with payload: '{Payload}'")]
    partial void LogDispatchStarted(TextCommandType type, string payload);

    [LoggerMessage(
        EventId = HandlerFound,
        Level = LogLevel.Debug,
        Message = ">> Handler identified: {Handler}. Executing...")]
    partial void LogHandlerFound(ICommandHandler handler);

    [LoggerMessage(
        EventId = HandlerNotFound,
        Level = LogLevel.Error,
        Message = "!! No handler found for command type: {CommandType}")]
    partial void LogHandlerNotFound(TextCommandType commandType);
}