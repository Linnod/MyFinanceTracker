namespace MyFinanceTracker.UseCases.Category.List;

public closed record ListCategoriesResponse : UseCaseResponse
{
    public sealed record Success(IReadOnlyList<Domain.Entities.Category> Categories)
     : ListCategoriesResponse
    {
        public override string ToString() => $"Found {Categories.Count} categories";
    }

    public sealed record Failure() : ListCategoriesResponse, IFailureResponse;
}
