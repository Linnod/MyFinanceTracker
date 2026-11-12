using MediatR;
using MyFinanceTracker.Domain.Repositories;

namespace MyFinanceTracker.UseCases.Category.Update;

internal sealed class UpdateCategoryHandler(ICategoryRepository categoryRepository)
    : IRequestHandler<UpdateCategoryRequest, UpdateCategoryResponse>
{
    public async Task<UpdateCategoryResponse> Handle(
        UpdateCategoryRequest request,
        CancellationToken ct)
    {
        var existing = await categoryRepository.GetById(request.Id, ct);
        if (existing is null)
        {
            return new UpdateCategoryResponse.ValidationError([
                new ValidationErrorItem(ValidationErrorCode.Category.CategoryNotFound, request.Id)
            ]);
        }

        var updated = new Domain.Entities.Category(
            existing.Id,
            string.IsNullOrWhiteSpace(request.Name) ? existing.Name : request.Name.Trim(),
            request.IsArchived ?? existing.IsArchived);

        await categoryRepository.Update(updated, ct);

        return new UpdateCategoryResponse.Success(updated);
    }
}
