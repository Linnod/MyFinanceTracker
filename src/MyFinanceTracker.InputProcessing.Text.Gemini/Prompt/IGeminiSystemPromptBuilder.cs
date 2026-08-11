namespace MyFinanceTracker.InputProcessing.Text.Gemini.Prompt;

internal interface IGeminiSystemPromptBuilder
{
    Task<string> BuildSystemInstructionAsync(CancellationToken ct = default);
}