using MediatR;

namespace MyFinanceTracker.UseCases.Category.List;

public sealed record ListCategoriesRequest(bool IncludeArchived = false) : IRequest<ListCategoriesResponse>
{
    public override string ToString() => "List all categories";
}
