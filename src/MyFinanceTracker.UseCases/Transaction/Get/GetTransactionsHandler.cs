using MediatR;
using MyFinanceTracker.Domain.Repositories;

namespace MyFinanceTracker.UseCases.Transaction.Get;

internal sealed class GetTransactionsHandler(
    ICategoryRepository categoryRepository,
    ITransactionRepository transactionRepository)
    : IRequestHandler<GetTransactionsRequest, GetTransactionsResponse>
{
    public async Task<GetTransactionsResponse> Handle(
        GetTransactionsRequest request,
        CancellationToken ct)
    {
        var (resolvedCategories, error) = await GetCategories(request.CategoryIds, ct);
        if (error is not null)
        {
            return error;
        }

        var filter = new TransactionFilter(
            request.FromDate,
            request.ToDate,
            resolvedCategories,
            request.Type,
            request.Skip,
            request.Take);

        var pagedResult = await transactionRepository.Get(filter, ct);

        var categoryNames = resolvedCategories is not null
            ? resolvedCategories.Select(c => c.Name).ToList()
            : [];

        return new GetTransactionsResponse.Success(
            request.FromDate,
            request.ToDate,
            categoryNames,
            pagedResult);
    }

    private async Task<(IReadOnlyList<Domain.Entities.Category>? Categories, GetTransactionsResponse? Error)> GetCategories(
        IReadOnlyList<string>? categoryIds,
        CancellationToken ct)
    {
        if (categoryIds is not { Count: > 0 })
        {
            return (null, null);
        }

        var foundCategories = await categoryRepository.GetByIds(categoryIds, ct);
        var categoriesById = foundCategories.ToDictionary(c => c.Id, StringComparer.OrdinalIgnoreCase);

        var errors = new List<ValidationErrorItem>();
        var matchedCategories = new List<Domain.Entities.Category>();

        foreach (var categoryId in categoryIds)
        {
            if (string.IsNullOrWhiteSpace(categoryId) ||
                !categoriesById.TryGetValue(categoryId.Trim(), out var category))
            {
                errors.Add(new ValidationErrorItem(
                    ValidationErrorCode.Transaction.CategoryNotFound,
                    categoryId));
            }
            else
            {
                matchedCategories.Add(category);
            }
        }

        if (errors.Count > 0)
        {
            return (null, new GetTransactionsResponse.ValidationError(errors));
        }

        IReadOnlyList<Domain.Entities.Category> resolvedCategories =
            [.. matchedCategories.DistinctBy(c => c.Id, StringComparer.OrdinalIgnoreCase)];

        return (resolvedCategories, null);
    }
}
