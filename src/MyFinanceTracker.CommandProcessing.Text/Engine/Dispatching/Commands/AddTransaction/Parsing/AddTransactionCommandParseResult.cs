namespace MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.AddTransaction.Parsing;

internal abstract record AddTransactionCommandParseResult
{
    private AddTransactionCommandParseResult() {}

    public sealed record Success(RawAddTransactionCommand Command) : AddTransactionCommandParseResult
    {
        public override string ToString() => Command.ToString();
    }

    public sealed record Failure(string Message) : AddTransactionCommandParseResult
    {
        public override string ToString() => $"Failure: {Message}";
    }
}