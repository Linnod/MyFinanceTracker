namespace MyFinanceTracker.InputProcessing.Gemini.Parsing;

internal sealed record DeleteTransactionsArgs(
    IReadOnlyList<string>? Ids,
    string RecognizedInput
)
{
    public IReadOnlyList<string> Ids { get; init; } = Ids ?? [];
}
