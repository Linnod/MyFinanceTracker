using MediatR;
using MyFinanceTracker.Domain.Repositories;

namespace MyFinanceTracker.UseCases.Category.List;

internal sealed class ListCategoriesHandler(ICategoryRepository categoryRepository)
 : IRequestHandler<ListCategoriesRequest, ListCategoriesResponse>
{
    public async Task<ListCategoriesResponse> Handle(ListCategoriesRequest request, CancellationToken ct)
    {
        var categories = await categoryRepository.GetAll(ct);

        if (!request.IncludeArchived)
        {
            categories = [.. categories.Where(c => !c.IsArchived)];
        }

        return new ListCategoriesResponse.Success(categories);
    }
}
