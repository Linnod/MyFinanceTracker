using MediatR;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.UseCases.Common;

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
        IReadOnlyList<Domain.Entities.Category>? resolvedCategories = null;

        if (request.CategoryAliases is { Count: > 0 })
        {
            var categories = await categoryRepository.GetAll(ct);
            var lookup = new CategoryLookup(categories);

            var errors = new List<ValidationErrorItem>();
            var foundCategories = new List<Domain.Entities.Category>();

            foreach (var alias in request.CategoryAliases)
            {
                var category = lookup.Resolve(alias);
                if (category is null)
                {
                    errors.Add(new ValidationErrorItem(
                        ValidationErrorCode.Transaction.CategoryNotFound,
                        alias));
                }
                else
                {
                    foundCategories.Add(category);
                }
            }

            if (errors.Count > 0)
            {
                return new GetTransactionsResponse.ValidationError(errors);
            }

            resolvedCategories = foundCategories.DistinctBy(c => c.Id).ToList();
        }

        var filter = new TransactionFilter(
            request.FromDate,
            request.ToDate,
            resolvedCategories,
            request.Type);

        var transactions = await transactionRepository.Get(filter, ct);

        var categoryNames = resolvedCategories is not null
            ? resolvedCategories.Select(c => c.Name).ToList()
            : [];

        return new GetTransactionsResponse.Success(
            request.FromDate,
            request.ToDate,
            categoryNames,
            request.Type,
            transactions);
    }
}
