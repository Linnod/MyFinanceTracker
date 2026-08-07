using Microsoft.Extensions.Logging;
using MyFinanceTracker.CommandProcessing.Text.Regex.Dispatching.Commands.DeleteTransaction.Parsing;

namespace MyFinanceTracker.CommandProcessing.Text.Regex.Dispatching.Commands.DeleteTransaction;

using static MyFinanceTracker.CommandProcessing.Text.Regex.Logging.LogEvents.DeleteCommandHandler;

internal sealed partial class DeleteTransactionCommandHandler
{
    [LoggerMessage(
        EventId = Entry,
        Level = LogLevel.Debug,
        Message = "--> Handling: '{Command}'")]
    partial void LogCommandHandlerEntry(DeleteTransactionCommand command);

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
        EventId = Exit,
        Level = LogLevel.Debug,
        Message = "<-- Finished with result: {Response}")]
    partial void LogCommandHandlerExit(TextCommandResponse response);
}