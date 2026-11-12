namespace MyFinanceTracker.UseCases.Category.Update;

public abstract record UpdateCategoryResponse : UseCaseResponse
{
    private UpdateCategoryResponse() { }

    public sealed record Success(Domain.Entities.Category Category) : UpdateCategoryResponse
    {
        public override string ToString() => $"Updated category {Category.Id}";
    }

    public sealed record ValidationError(IReadOnlyCollection<ValidationErrorItem> Errors)
        : UpdateCategoryResponse, IValidationErrorResponse
    {
        public override string ToString() => string.Join("; ", Errors);
    }

    public sealed record Failure : UpdateCategoryResponse, IFailureResponse
    {
        public override string ToString() => "Failed to update category";
    }
}
