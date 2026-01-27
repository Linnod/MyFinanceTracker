namespace MyFinanceTracker.UseCases.Transaction.Create;

public abstract record CreateTransactionResponse
{
    private CreateTransactionResponse() { }

    public sealed record Success(
        string CategoryName,
        IReadOnlyCollection<decimal> Amounts,
        DateOnly Date,
        string? Note
    ) : CreateTransactionResponse;

    public sealed record ValidationError(string Message) : CreateTransactionResponse;

    public sealed record Failure(string Message) : CreateTransactionResponse;
}