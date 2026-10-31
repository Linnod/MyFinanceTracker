using Microsoft.Extensions.Logging;
using MyFinanceTracker.InputProcessing;

namespace MyFinanceTracker.InputProcessing.Text.Structured;

using static MyFinanceTracker.InputProcessing.Text.Structured.Logging.LogEvents.Processor;

internal sealed partial class StructuredTextInputProcessor
{
    [LoggerMessage(
        EventId = Entry,
        Level = LogLevel.Debug,
        Message = "Executing input '{Input}'")]
    partial void LogExecuteEntry(Input.Text input);

    [LoggerMessage(
        EventId = Exit,
        Level = LogLevel.Debug,
        Message = "Execution finished with result: {Result}")]
    partial void LogExecuteExit(ProcessingResult result);
}
