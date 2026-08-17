namespace MyFinanceTracker.UseCases.Transaction.Get;

public abstract record GetTransactionsResponse
{
    private GetTransactionsResponse() { }

    public sealed record Success(
        string CategoryName,
        DateOnly Date,
        IReadOnlyList<Domain.Entities.Transaction> Transactions
    ) : GetTransactionsResponse
    {
        public override string ToString() =>
            $"Got {Transactions.Count} transaction(s) for '{CategoryName}' on {Date:dd.MM.yyyy}";
    }

    public sealed record ValidationError(
        IReadOnlyCollection<ValidationErrorItem> Errors
    ) : GetTransactionsResponse
    {
        public override string ToString() => Errors.Count == 0
            ? "No validation errors"
            : string.Join("; ", Errors.Select(e => e.ErrorCode));
    }

    public sealed record Failure() : GetTransactionsResponse
    {
        public override string ToString() => "Failed to get transactions";
    }
}