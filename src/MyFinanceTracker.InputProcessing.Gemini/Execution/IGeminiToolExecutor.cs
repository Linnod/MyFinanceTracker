using Google.GenAI.Types;

namespace MyFinanceTracker.InputProcessing.Gemini.Execution;

internal interface IGeminiToolExecutor
{
    Task<ActionResult> ExecuteToolCall(FunctionCall functionCall, CancellationToken ct);
}