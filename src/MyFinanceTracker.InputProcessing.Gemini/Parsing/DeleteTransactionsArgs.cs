namespace MyFinanceTracker.InputProcessing.Gemini.Parsing;

internal sealed record DeleteTransactionsArgs(
    IReadOnlyList<string>? Ids
)
{
    public IReadOnlyList<string> Ids { get; init; } = Ids ?? [];
}
