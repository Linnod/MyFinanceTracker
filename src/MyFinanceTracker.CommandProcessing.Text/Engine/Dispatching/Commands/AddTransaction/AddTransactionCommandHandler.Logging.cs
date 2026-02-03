using Microsoft.Extensions.Logging;
using MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.AddTransaction.Parsing;

namespace MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.AddTransaction;

using static MyFinanceTracker.CommandProcessing.Text.Logging.LogEvents.AddCommandHandler;

internal sealed partial class AddTransactionCommandHandler
{
    [LoggerMessage(
        EventId = Entry,
        Level = LogLevel.Debug,
        Message = "--> Handling payload: '{Payload}'")]
    partial void LogHandlerEntry(string payload);

    [LoggerMessage(
        EventId = ParseSuccess,
        Level = LogLevel.Debug,
        Message = "++ Parse successful: {Result}")]
    partial void LogParseSuccess(AddTransactionCommandParseResult result);

    [LoggerMessage(
        EventId = ParseFailure,
        Level = LogLevel.Information,
        Message = "!! Parse failed: {Result}")]
    partial void LogParseFailure(AddTransactionCommandParseResult result);

    [LoggerMessage(
        EventId = SystemError,
        Level = LogLevel.Error,
        Message = "!! System failure for input: {Input}")]
    partial void LogSystemError(string input, Exception ex);

    [LoggerMessage(
        EventId = Exit,
        Level = LogLevel.Debug,
        Message = "<-- Finished with result: {Response}")]
    partial void LogHandlerExit(TextCommandResponse response);
}