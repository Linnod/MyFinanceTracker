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
        var categories = await categoryRepository.GetAll(ct);
        var lookup = new CategoryLookup(categories);

        return lookup.Resolve(request.CategoryAlias!) switch
        {
            CategoryResolution.NotFound notFound => new GetTransactionsResponse.ValidationError([
                new ValidationErrorItem(ValidationErrorCode.Transaction.CategoryNotFound, notFound.Suggestion)
            ]),
            CategoryResolution.Found found => await GetTransactions(found.Category, request.Date!.Value, ct),
            _ => throw new UnreachableException()
        };
    }

    private async Task<GetTransactionsResponse> GetTransactions(
        Domain.Entities.Category category,
        DateOnly date,
        CancellationToken ct)
    {
        var transactions = await transactionRepository.Get(category, date, ct);

        return new GetTransactionsResponse.Success(category.Name, date, transactions);
    }
}