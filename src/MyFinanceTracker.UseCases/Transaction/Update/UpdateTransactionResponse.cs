namespace MyFinanceTracker.UseCases.Transaction.Update;

public abstract record UpdateTransactionResponse : UseCaseResponse
{
    private UpdateTransactionResponse() { }

    public sealed record Success(Domain.Entities.Transaction Transaction) : UpdateTransactionResponse
    {
        public override string ToString() => $"Updated transaction {Transaction.Id}";
    }

    public sealed record ValidationError(IReadOnlyCollection<ValidationErrorItem> Errors)
        : UpdateTransactionResponse, IValidationErrorResponse
    {
        public override string ToString() => Errors.Count == 0
            ? "No validation errors"
            : string.Join("; ", Errors.Select(e => e.ErrorCode));
    }

    public sealed record Failure : UpdateTransactionResponse, IFailureResponse
    {
        public override string ToString() => "Failed to update transaction";
    }
}
