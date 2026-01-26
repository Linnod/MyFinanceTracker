namespace MyFinanceTracker.Interactions.Commands.DeleteTransaction.Parsing;

internal abstract record DeleteTransactionCommandParseResult
{
    private DeleteTransactionCommandParseResult() { }

    public sealed record Success(RawDeleteTransactionCommand Command) : DeleteTransactionCommandParseResult;
    public sealed record EmptyInput : DeleteTransactionCommandParseResult;
    public sealed record InvalidFormat : DeleteTransactionCommandParseResult;
    public sealed record UnparseableDate(string Value) : DeleteTransactionCommandParseResult;
}