namespace MyFinanceTracker.Domain.Repositories;

public abstract record DeleteTransactionsResult
{
    private DeleteTransactionsResult() { }

    public sealed record Success(int DeletedCount) : DeleteTransactionsResult;

    public sealed record NotFound(IReadOnlyCollection<Guid> MissingIds) : DeleteTransactionsResult;
}
