using MediatR;
using MyFinanceTracker.Domain.Repositories;

namespace MyFinanceTracker.UseCases.Category.Create;

internal sealed class CreateCategoryHandler(ICategoryRepository categoryRepository)
    : IRequestHandler<CreateCategoryRequest, CreateCategoryResponse>
{
    public async Task<CreateCategoryResponse> Handle(
        CreateCategoryRequest request,
        CancellationToken ct)
    {
        var existing = await categoryRepository.GetById(request.Id, ct);
        if (existing is not null)
        {
            return new CreateCategoryResponse.ValidationError([
                new ValidationErrorItem(ValidationErrorCode.Category.CategoryAlreadyExists, request.Id)
            ]);
        }

        var category = new Domain.Entities.Category(
            request.Id.Trim().ToLowerInvariant(),
            request.Name.Trim(),
            isArchived: false);

        await categoryRepository.Add(category, ct);

        return new CreateCategoryResponse.Success(category);
    }
}
