namespace MyFinanceTracker.InputProcessing.Text.Structured.Dispatching.Commands.DeleteTransaction.Parsing;

internal abstract record DeleteTransactionCommandParseResult
{
    private DeleteTransactionCommandParseResult() { }

    public sealed record Success(DeleteTransactionParsedPayload Payload) : DeleteTransactionCommandParseResult;

    public sealed record Failure(string ErrorCode) : DeleteTransactionCommandParseResult
    {
        public override string ToString() => $"Failure: {ErrorCode}";
    }
}