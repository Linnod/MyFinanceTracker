namespace MyFinanceTracker.UseCases.Transaction.Delete;

public abstract record DeleteTransactionsResponse : UseCaseResponse
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
    ) : DeleteTransactionsResponse, IValidationErrorResponse
    {
        public override string ToString() => string.Join("; ", Errors);
    }

    public sealed record Failure() : DeleteTransactionsResponse, IFailureResponse
    {
        public override string ToString() => "Failed to delete transactions";
    }
}
