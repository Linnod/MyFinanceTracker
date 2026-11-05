using MediatR;
using MyFinanceTracker.Domain.Repositories;

namespace MyFinanceTracker.UseCases.Category.List;

internal sealed class ListCategoriesCommandHandler(ICategoryRepository categoryRepository)
 : IRequestHandler<ListCategoriesRequest, ListCategoriesResponse>
{
    public async Task<ListCategoriesResponse> Handle(ListCategoriesRequest request, CancellationToken cancellationToken)
    {
        var categories = await categoryRepository.GetAll(cancellationToken);

        if (!request.IncludeArchived)
        {
            categories = [.. categories.Where(c => !c.IsArchived)];
        }

        return new ListCategoriesResponse.Success(categories);
    }
}
