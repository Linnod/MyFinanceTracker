using Microsoft.Extensions.Logging;
using MyFinanceTracker.InputProcessing.Text.Gemini.Logging;

namespace MyFinanceTracker.InputProcessing.Text.Gemini.Execution;

internal sealed partial class GeminiToolExecutor
{
    [LoggerMessage(
        EventId = LogEvents.Executor.ExecutingTool,
        Level = LogLevel.Information,
        Message = "Executing tool call '{ToolName}'")]
    private partial void LogExecutingTool(string? toolName);

    [LoggerMessage(
        EventId = LogEvents.Executor.ExecutedTool,
        Level = LogLevel.Information,
        Message = "Executed tool call '{ToolName}'")]
    private partial void LogExecutedTool(string? toolName);
}