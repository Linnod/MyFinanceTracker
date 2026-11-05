using Google.GenAI.Types;

namespace MyFinanceTracker.InputProcessing.Gemini.Declarations;

internal interface IGeminiToolDeclarationProvider
{
    IReadOnlyList<Tool> GetToolDeclarations();
}