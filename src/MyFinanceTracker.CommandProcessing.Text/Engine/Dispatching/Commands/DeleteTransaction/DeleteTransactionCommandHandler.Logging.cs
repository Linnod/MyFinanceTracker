using Microsoft.Extensions.Logging;
using MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.DeleteTransaction.Parsing;

namespace MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.DeleteTransaction;

using static MyFinanceTracker.CommandProcessing.Text.Logging.LogEvents.DeleteCommandHandler;

internal sealed partial class DeleteTransactionCommandHandler
{
    [LoggerMessage(
        EventId = Entry,
        Level = LogLevel.Debug,
        Message = "--> Handling payload: '{Payload}'")]
    partial void LogCommandHandlerEntry(string payload);

    [LoggerMessage(
        EventId = ParseSuccess,
        Level = LogLevel.Debug,
        Message = "++ Parse successful: {Result}")]
    partial void LogParseSuccess(DeleteTransactionCommandParseResult result);

    [LoggerMessage(
        EventId = ParseFailure,
        Level = LogLevel.Information,
        Message = "!! Parse failed: {Result}")]
    partial void LogParseFailure(DeleteTransactionCommandParseResult result);

    [LoggerMessage(
        EventId = SystemError,
        Level = LogLevel.Error,
        Message = "!! System failure while deleting for input: {Input}")]
    partial void LogSystemError(string input, Exception ex);

    [LoggerMessage(
        EventId = Exit,
        Level = LogLevel.Debug,
        Message = "<-- Finished with result: {Response}")]
    partial void LogCommandHandlerExit(TextCommandResponse response);
}