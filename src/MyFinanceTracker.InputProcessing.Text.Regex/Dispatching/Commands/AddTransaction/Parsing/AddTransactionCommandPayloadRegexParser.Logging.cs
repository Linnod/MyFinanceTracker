using Microsoft.Extensions.Logging;
using MyFinanceTracker.InputProcessing.Text.Regex.Logging;

namespace MyFinanceTracker.InputProcessing.Text.Regex.Dispatching.Commands.AddTransaction.Parsing;
    
internal sealed partial class AddTransactionCommandPayloadRegexParser
{
    [LoggerMessage(
        EventId = LogEvents.Commands.Add.Parser.ParseSuccess,
        Level = LogLevel.Debug,
        Message = "++ Parse successful: {Result}")]
    partial void LogParseSuccess(AddTransactionCommandParseResult result);

    [LoggerMessage(
        EventId = LogEvents.Commands.Add.Parser.ParseFailure,
        Level = LogLevel.Information,
        Message = "!! Parse failed: {Result}")]
    partial void LogParseFailure(AddTransactionCommandParseResult result);
}