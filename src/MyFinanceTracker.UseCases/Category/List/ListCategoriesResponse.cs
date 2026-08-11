namespace MyFinanceTracker.UseCases.Category.List;

public abstract record ListCategoriesResponse
{
    private ListCategoriesResponse() { }

    public sealed record Success(IReadOnlyList<Domain.Entities.Category> Categories)
     : ListCategoriesResponse
    {
        public override string ToString() => $"Found {Categories.Count} categories";
    }

    public sealed record Failure() : ListCategoriesResponse;
}
