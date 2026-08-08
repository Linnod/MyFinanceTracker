using Google.GenAI.Types;

namespace MyFinanceTracker.CommandProcessing.Text.Gemini.Declarations;

internal interface IGeminiToolDeclarationProvider
{
    IReadOnlyList<Tool> GetToolDeclarations();
}