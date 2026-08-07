using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.CommandProcessing.Text.Regex;

using static MyFinanceTracker.CommandProcessing.Text.Regex.Logging.LogEvents.Processor;

internal sealed partial class RegexTextCommandProcessor
{
    [LoggerMessage(
        EventId = Entry,
        Level = LogLevel.Debug,
        Message = "--> Executing request: '{Request}'")]
    partial void LogExecuteEntry(TextCommandRequest request);

    [LoggerMessage(
        EventId = Exit,
        Level = LogLevel.Debug,
        Message = "<-- Execution finished with: {Response}")]
    partial void LogExecuteExit(TextCommandResponse response);
}