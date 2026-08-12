namespace MyFinanceTracker.InputProcessing.Text.Gemini.Prompt;

internal interface IGeminiSystemPromptBuilder
{
    Task<string> BuildSystemInstruction(CancellationToken ct);
}