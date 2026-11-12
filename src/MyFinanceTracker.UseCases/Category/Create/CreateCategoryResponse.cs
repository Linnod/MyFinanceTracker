namespace MyFinanceTracker.UseCases.Category.Create;

public abstract record CreateCategoryResponse : UseCaseResponse
{
    private CreateCategoryResponse() { }

    public sealed record Success(Domain.Entities.Category Category) : CreateCategoryResponse
    {
        public override string ToString() => $"Created category {Category.Id}";
    }

    public sealed record ValidationError(IReadOnlyCollection<ValidationErrorItem> Errors)
        : CreateCategoryResponse, IValidationErrorResponse
    {
        public override string ToString() => string.Join("; ", Errors);
    }

    public sealed record Failure : CreateCategoryResponse, IFailureResponse
    {
        public override string ToString() => "Failed to create category";
    }
}
