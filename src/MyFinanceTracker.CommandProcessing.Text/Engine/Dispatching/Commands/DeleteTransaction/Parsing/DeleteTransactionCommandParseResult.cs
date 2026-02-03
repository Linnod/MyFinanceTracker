namespace MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.DeleteTransaction.Parsing;

internal abstract record DeleteTransactionCommandParseResult
{
    private DeleteTransactionCommandParseResult() { }

    public sealed record Success(RawDeleteTransactionCommand Command) : DeleteTransactionCommandParseResult;

    public sealed record Failure(
        string Reason,
        string Suggestion,
        IReadOnlyCollection<string> Examples
    ) : DeleteTransactionCommandParseResult
    {
        public override string ToString()
        {
            return $"[Failure] Reason: {Reason}";
        }
    }
}