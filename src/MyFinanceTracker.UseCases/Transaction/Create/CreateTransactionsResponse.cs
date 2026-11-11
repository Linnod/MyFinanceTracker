namespace MyFinanceTracker.UseCases.Transaction.Create;

public abstract record CreateTransactionsResponse : UseCaseResponse
{
    private CreateTransactionsResponse() { }

    public sealed record Success(
        IReadOnlyList<Domain.Entities.Transaction> Transactions
    ) : CreateTransactionsResponse
    {
        public override string ToString() =>
            $"Created transactions: [{string.Join(", ", Transactions.Select(t => t.Id))}]";
    }

    public sealed record ValidationError(
        IReadOnlyCollection<ValidationErrorItem> Errors
    ) : CreateTransactionsResponse, IValidationErrorResponse
    {
        public override string ToString() => string.Join("; ", Errors);
    }

    public sealed record Failure() : CreateTransactionsResponse, IFailureResponse
    {
        public override string ToString() => "Failed to process transactions";
    }
}
