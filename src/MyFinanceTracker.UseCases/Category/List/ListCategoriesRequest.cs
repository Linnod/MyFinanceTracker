using MediatR;

namespace MyFinanceTracker.UseCases.Category.List;

public class ListCategoriesRequest : IRequest<ListCategoriesResponse>
{
    public override string ToString() => "List all categories";
}
