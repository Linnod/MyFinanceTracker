namespace MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.DeleteTransaction.Parsing;

internal abstract record DeleteTransactionCommandParseResult
{
    private DeleteTransactionCommandParseResult() { }

    public sealed record Success(RawDeleteTransactionCommand Command) : DeleteTransactionCommandParseResult
    {
        public override string ToString() => Command.ToString();
    }

    public sealed record Failure(string Message) : DeleteTransactionCommandParseResult
    {
        public override string ToString() => $"Failure: {Message}";
    }
}