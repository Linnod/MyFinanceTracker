namespace MyFinanceTracker.InputProcessing.Gemini.Prompt;

internal interface IGeminiSystemPromptBuilder
{
    Task<string> BuildSystemInstruction(CancellationToken ct);
}