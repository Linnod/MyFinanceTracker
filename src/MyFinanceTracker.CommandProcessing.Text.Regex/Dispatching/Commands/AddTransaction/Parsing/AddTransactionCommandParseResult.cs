namespace MyFinanceTracker.CommandProcessing.Text.Regex.Dispatching.Commands.AddTransaction.Parsing;

internal abstract record AddTransactionCommandParseResult
{
    private AddTransactionCommandParseResult() { }

    public sealed record Success(AddTransactionParsedPayload Payload) : AddTransactionCommandParseResult
    {
        public override string ToString() => Payload.ToString();
    }

    public sealed record Failure(string Reason) : AddTransactionCommandParseResult
    {
        public override string ToString()
        {
            return $"[Failure] Reason: {Reason}";
        }
    }
}