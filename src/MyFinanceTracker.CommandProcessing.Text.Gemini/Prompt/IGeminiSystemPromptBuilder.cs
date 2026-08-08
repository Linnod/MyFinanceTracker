namespace MyFinanceTracker.CommandProcessing.Text.Gemini.Prompt;

internal interface IGeminiSystemPromptBuilder
{
    Task<string> BuildSystemInstructionAsync(CancellationToken ct = default);
}