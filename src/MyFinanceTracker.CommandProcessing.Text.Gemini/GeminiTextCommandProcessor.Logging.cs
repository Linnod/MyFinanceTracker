using Microsoft.Extensions.Logging;
using MyFinanceTracker.CommandProcessing.Text.Gemini.Logging;

namespace MyFinanceTracker.CommandProcessing.Text.Gemini;

internal sealed partial class GeminiTextCommandProcessor
{
    [LoggerMessage(
        EventId = LogEvents.Processor.Entry,
        Level = LogLevel.Information,
        Message = "Executing request: '{Request}'")]
    private partial void LogExecuteEntry(TextCommandRequest request);

    [LoggerMessage(
        EventId = LogEvents.Processor.TextResponse,
        Level = LogLevel.Information,
        Message = "Gemini returned text response without tool calls.")]
    private partial void LogTextResponse();

    [LoggerMessage(
        EventId = LogEvents.Processor.TextResponse,
        Level = LogLevel.Warning,
        Message = "Gemini returned neither text nor tool calls.")]
    private partial void LogEmptyResponse();

    [LoggerMessage(
        EventId = LogEvents.Processor.Exit,
        Level = LogLevel.Debug,
        Message = "Execution finished with: {Response}")]
    private partial void LogExecuteExit(TextCommandResponse response);

    [LoggerMessage(
        EventId = LogEvents.Processor.Error,
        Level = LogLevel.Error,
        Message = "Gemini API execution failed unexpectedly.")]
    private partial void LogError(Exception exception);
}