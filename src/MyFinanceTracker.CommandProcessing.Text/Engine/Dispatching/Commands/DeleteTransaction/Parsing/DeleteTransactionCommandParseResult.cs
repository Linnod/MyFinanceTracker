namespace MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.DeleteTransaction.Parsing;

internal abstract record DeleteTransactionCommandParseResult
{
    private DeleteTransactionCommandParseResult() { }

    public sealed record Success(DeleteTransactionParsedPayload Payload) : DeleteTransactionCommandParseResult;

    public sealed record Failure(string Reason) : DeleteTransactionCommandParseResult
    {
        public override string ToString()
        {
            return $"[Failure] Reason: {Reason}";
        }
    }
}