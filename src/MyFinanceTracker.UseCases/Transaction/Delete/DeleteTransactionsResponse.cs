namespace MyFinanceTracker.UseCases.Transaction.Delete;

public abstract record DeleteTransactionsResponse
{
    private DeleteTransactionsResponse() { }

    public sealed record Success(
        int DeletedCount
    ) : DeleteTransactionsResponse
    {
        public override string ToString() => $"Deleted {DeletedCount} transaction(s)";
    }

    public sealed record ValidationError(
        IReadOnlyCollection<ValidationErrorItem> Errors
    ) : DeleteTransactionsResponse
    {
        public override string ToString() => Errors.Count == 0
            ? "No validation errors"
            : string.Join("; ", Errors.Select(e => e.ErrorCode));
    }

    public sealed record Failure() : DeleteTransactionsResponse
    {
        public override string ToString() => "Failed to delete transactions";
    }
}
