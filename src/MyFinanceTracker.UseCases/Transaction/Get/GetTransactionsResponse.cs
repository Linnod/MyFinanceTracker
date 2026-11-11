using MyFinanceTracker.Domain.Common;

namespace MyFinanceTracker.UseCases.Transaction.Get;

public abstract record GetTransactionsResponse : UseCaseResponse
{
    private GetTransactionsResponse() { }

    public sealed record Success(
        DateTime FromDate,
        DateTime ToDate,
        IReadOnlyList<string> CategoryNames,
        PagedResult<Domain.Entities.Transaction> Transactions
    ) : GetTransactionsResponse
    {
        public override string ToString() =>
            $"Got {Transactions.Items.Count} of {Transactions.TotalCount} transaction(s) from {FromDate:dd.MM.yyyy HH:mm:ss} to {ToDate:dd.MM.yyyy HH:mm:ss}";
    }

    public sealed record ValidationError(
        IReadOnlyCollection<ValidationErrorItem> Errors
    ) : GetTransactionsResponse, IValidationErrorResponse
    {
        public override string ToString() => string.Join("; ", Errors);
    }

    public sealed record Failure() : GetTransactionsResponse, IFailureResponse
    {
        public override string ToString() => "Failed to get transactions";
    }
}
