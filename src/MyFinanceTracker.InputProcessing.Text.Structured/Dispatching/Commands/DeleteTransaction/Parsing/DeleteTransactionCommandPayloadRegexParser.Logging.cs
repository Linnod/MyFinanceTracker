using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.InputProcessing.Text.Structured.Dispatching.Commands.DeleteTransaction.Parsing;

using static MyFinanceTracker.InputProcessing.Text.Structured.Logging.LogEvents.Commands.Delete.Parser;

internal sealed partial class DeleteTransactionCommandPayloadRegexParser
{
    [LoggerMessage(
        EventId = ParseSuccess,
        Level = LogLevel.Debug,
        Message = "Successfully parsed delete transaction payload: {Result}")]
    partial void LogParseSuccess(DeleteTransactionCommandParseResult result);

    [LoggerMessage(
        EventId = ParseFailure,
        Level = LogLevel.Information,
        Message = "Failed to parse delete transaction payload: {Result}")]
    partial void LogParseFailure(DeleteTransactionCommandParseResult result);
}