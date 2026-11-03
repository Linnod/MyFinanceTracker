namespace MyFinanceTracker.InputProcessing.Text.Gemini.Parsing;

internal sealed record DeleteTransactionsArgs(
    IReadOnlyList<Guid>? Ids,
    string RecognizedInput
)
{
    public IReadOnlyList<Guid> Ids { get; init; } = Ids ?? [];
}
