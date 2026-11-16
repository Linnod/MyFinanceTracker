namespace MyFinanceTracker.UseCases.Transaction.GetById;

public closed record GetTransactionByIdResponse : UseCaseResponse
{
    public sealed record Success(Domain.Entities.Transaction Transaction) : GetTransactionByIdResponse
    {
        public override string ToString() => $"Got transaction {Transaction.Id}";
    }

    public sealed record ValidationError(
        IReadOnlyCollection<ValidationErrorItem> Errors
    ) : GetTransactionByIdResponse, IValidationErrorResponse
    {
        public override string ToString() => string.Join("; ", Errors);
    }

    public sealed record Failure() : GetTransactionByIdResponse, IFailureResponse
    {
        public override string ToString() => "Failed to get transaction";
    }
}
