namespace MyFinanceTracker.UseCases.Transaction.Get;

public abstract record GetTransactionsResponse : UseCaseResponse
{
    private GetTransactionsResponse() { }

    public sealed record Success(
        DateTime FromDate,
        DateTime ToDate,
        IReadOnlyList<string> CategoryNames,
        IReadOnlyList<Domain.Entities.Transaction> Transactions
    ) : GetTransactionsResponse
    {
        public override string ToString() =>
            $"Got {Transactions.Count} transaction(s) from {FromDate:dd.MM.yyyy HH:mm:ss} to {ToDate:dd.MM.yyyy HH:mm:ss}";
    }

    public sealed record ValidationError(
        IReadOnlyCollection<ValidationErrorItem> Errors
    ) : GetTransactionsResponse, IValidationErrorResponse
    {
        public override string ToString() => Errors.Count == 0
            ? "No validation errors"
            : string.Join("; ", Errors.Select(e => e.ErrorCode));
    }

    public sealed record Failure() : GetTransactionsResponse, IFailureResponse
    {
        public override string ToString() => "Failed to get transactions";
    }
}
