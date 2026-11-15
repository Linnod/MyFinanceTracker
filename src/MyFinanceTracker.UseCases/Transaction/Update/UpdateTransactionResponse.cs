namespace MyFinanceTracker.UseCases.Transaction.Update;

public closed record UpdateTransactionResponse : UseCaseResponse
{
    public sealed record Success(Domain.Entities.Transaction Transaction) : UpdateTransactionResponse
    {
        public override string ToString() => $"Updated transaction {Transaction.Id}";
    }

    public sealed record ValidationError(IReadOnlyCollection<ValidationErrorItem> Errors)
        : UpdateTransactionResponse, IValidationErrorResponse
    {
        public override string ToString() => string.Join("; ", Errors);
    }

    public sealed record Failure : UpdateTransactionResponse, IFailureResponse
    {
        public override string ToString() => "Failed to update transaction";
    }
}
