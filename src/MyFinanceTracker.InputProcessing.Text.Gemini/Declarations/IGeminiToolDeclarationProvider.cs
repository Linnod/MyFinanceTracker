using Google.GenAI.Types;

namespace MyFinanceTracker.InputProcessing.Text.Gemini.Declarations;

internal interface IGeminiToolDeclarationProvider
{
    IReadOnlyList<Tool> GetToolDeclarations();
}