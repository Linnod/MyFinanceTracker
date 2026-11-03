using System.Diagnostics;
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
                switch (lookup.Resolve(alias))
                {
                    case CategoryResolution.NotFound notFound:
                        errors.Add(new ValidationErrorItem(
                            ValidationErrorCode.Transaction.CategoryNotFound,
                            notFound.Suggestion,
                            alias));
                        break;
                    case CategoryResolution.Found found:
                        foundCategories.Add(found.Category);
                        break;
                    default:
                        throw new UnreachableException();
                }
            }

            if (errors.Count > 0)
            {
                return new GetTransactionsResponse.ValidationError(errors);
            }

            resolvedCategories = foundCategories.DistinctBy(c => c.Id).ToList();
        }

        var filter = new TransactionFilter(
            request.FromDate!.Value,
            request.ToDate!.Value,
            resolvedCategories,
            request.Type);

        var transactions = await transactionRepository.Get(filter, ct);

        var categoryNames = resolvedCategories is not null
            ? resolvedCategories.Select(c => c.Name).ToList()
            : [];

        return new GetTransactionsResponse.Success(
            request.FromDate.Value,
            request.ToDate.Value,
            categoryNames,
            request.Type,
            transactions);
    }
}
