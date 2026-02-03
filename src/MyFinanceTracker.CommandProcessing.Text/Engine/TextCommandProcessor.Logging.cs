using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.CommandProcessing.Text.Engine;

using static MyFinanceTracker.CommandProcessing.Text.Logging.LogEvents.Processor;

internal sealed partial class TextCommandProcessor
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