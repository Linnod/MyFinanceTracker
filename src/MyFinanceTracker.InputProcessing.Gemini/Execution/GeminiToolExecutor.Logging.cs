using Microsoft.Extensions.Logging;
using MyFinanceTracker.InputProcessing.Gemini.Logging;
using MyFinanceTracker.InputProcessing.Gemini.Parsing;

namespace MyFinanceTracker.InputProcessing.Gemini.Execution;

internal sealed partial class GeminiToolExecutor
{
    [LoggerMessage(
        EventId = LogEvents.Executor.ExecutingTool,
        Level = LogLevel.Debug,
        Message = "Executing tool call '{ToolName}' with args {ToolArgs}")]
    private partial void LogExecutingTool(string? toolName, ToolCallArgs toolArgs);

    [LoggerMessage(
        EventId = LogEvents.Executor.ExecutedTool,
        Level = LogLevel.Debug,
        Message = "Executed tool call '{ToolName}'")]
    private partial void LogExecutedTool(string? toolName);
}