namespace MyFinanceTracker.UseCases.Transaction.Delete;

public abstract record DeleteTransactionsResponse
{
    private DeleteTransactionsResponse() { }

    public sealed record Success(string CategoryName, DateOnly Date) : DeleteTransactionsResponse;
    public sealed record ValidationError(string Message) : DeleteTransactionsResponse;
    public sealed record Failure(string Message) : DeleteTransactionsResponse;
}