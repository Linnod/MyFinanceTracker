using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.InputProcessing.Text.Regex.Dispatching.Commands.DeleteTransaction.Parsing;

using static MyFinanceTracker.InputProcessing.Text.Regex.Logging.LogEvents.Commands.Delete.Parser;

internal sealed partial class DeleteTransactionCommandPayloadRegexParser
{
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
}