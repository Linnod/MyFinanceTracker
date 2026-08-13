using Microsoft.Extensions.Logging;
using MyFinanceTracker.InputProcessing.Text.Structured.Logging;

namespace MyFinanceTracker.InputProcessing.Text.Structured.Dispatching.Commands.AddTransaction.Parsing;

internal sealed partial class AddTransactionCommandPayloadRegexParser
{
    [LoggerMessage(
        EventId = LogEvents.Commands.Add.Parser.ParseSuccess,
        Level = LogLevel.Debug,
        Message = "Successfully parsed add transaction payload: {Result}")]
    partial void LogParseSuccess(AddTransactionCommandParseResult result);

    [LoggerMessage(
        EventId = LogEvents.Commands.Add.Parser.ParseFailure,
        Level = LogLevel.Information,
        Message = "Failed to parse add transaction payload: {Result}")]
    partial void LogParseFailure(AddTransactionCommandParseResult result);
}