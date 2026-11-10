using Google.GenAI.Types;
using MyFinanceTracker.UseCases;

namespace MyFinanceTracker.InputProcessing.Gemini.Execution;

internal interface IGeminiToolExecutor
{
    Task<UseCaseResponse> ExecuteToolCall(FunctionCall functionCall, CancellationToken ct);
}