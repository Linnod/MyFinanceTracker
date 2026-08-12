using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.InputProcessing.Text.Regex;

using static MyFinanceTracker.InputProcessing.Text.Regex.Logging.LogEvents.Processor;

internal sealed partial class RegexTextInputProcessor
{
    [LoggerMessage(
        EventId = Entry,
        Level = LogLevel.Debug,
        Message = "Executing input '{Input}'")]
    partial void LogExecuteEntry(TextInput input);

    [LoggerMessage(
        EventId = Exit,
        Level = LogLevel.Debug,
        Message = "Execution finished with result: {Result}")]
    partial void LogExecuteExit(ProcessingResult result);
}