namespace MyFinanceTracker.Interactions.Commands.AddTransaction.Parsing;

internal abstract record AddTransactionCommandParseResult
{
    private AddTransactionCommandParseResult() { }

    public sealed record Success(RawAddTransactionCommand Data) : AddTransactionCommandParseResult;

    public sealed record EmptyInput : AddTransactionCommandParseResult;
    public sealed record MissingTransactionType : AddTransactionCommandParseResult;
    public sealed record InvalidFormat : AddTransactionCommandParseResult;
    public sealed record InvalidAmount(string RawValue) : AddTransactionCommandParseResult;

    public sealed record UnparseableDate(string RawValue) : AddTransactionCommandParseResult;
}