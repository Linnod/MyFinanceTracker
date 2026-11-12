namespace MyFinanceTracker.InputProcessing.Gemini.Parsing;

internal sealed record UpdateCategoryArgs(
    string? Id,
    string? Name,
    bool? IsArchived);
