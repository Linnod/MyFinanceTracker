using Microsoft.Extensions.Logging;
using MyFinanceTracker.InputProcessing.Text.Gemini.Logging;

namespace MyFinanceTracker.InputProcessing.Text.Gemini;

internal sealed partial class GeminiTextInputProcessor
{
    [LoggerMessage(
        EventId = LogEvents.Processor.Entry,
        Level = LogLevel.Information,
        Message = "Executing request: '{Input}'")]
    private partial void LogExecuteEntry(TextInput input);

    [LoggerMessage(
        EventId = LogEvents.Processor.TextResponse,
        Level = LogLevel.Information,
        Message = "Gemini returned text response without tool calls.")]
    private partial void LogTextResponse();

    [LoggerMessage(
        EventId = LogEvents.Processor.EmptyResponse,
        Level = LogLevel.Warning,
        Message = "Gemini returned neither text nor tool calls.")]
    private partial void LogEmptyResponse();

    [LoggerMessage(
        EventId = LogEvents.Processor.Exit,
        Level = LogLevel.Debug,
        Message = "Execution finished with: {Result}")]
    private partial void LogExecuteExit(ProcessingResult result);

    [LoggerMessage(
        EventId = LogEvents.Processor.Error,
        Level = LogLevel.Error,
        Message = "Gemini API execution failed unexpectedly.")]
    private partial void LogError(Exception exception);
}