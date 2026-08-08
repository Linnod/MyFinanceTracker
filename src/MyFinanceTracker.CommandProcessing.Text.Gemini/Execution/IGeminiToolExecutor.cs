using Google.GenAI.Types;

namespace MyFinanceTracker.CommandProcessing.Text.Gemini.Execution;

internal interface IGeminiToolExecutor
{
    Task<TextCommandResponse> ExecuteToolCallAsync(FunctionCall functionCall, CancellationToken ct = default);
}