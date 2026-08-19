using System.Diagnostics;
using MediatR;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.UseCases.Common;

namespace MyFinanceTracker.UseCases.Transaction.Delete;

internal sealed class DeleteTransactionsHandler(
    ICategoryRepository categoryRepository,
    ITransactionRepository transactionRepository)
    : IRequestHandler<DeleteTransactionsRequest, DeleteTransactionsResponse>
{
    public async Task<DeleteTransactionsResponse> Handle(
    DeleteTransactionsRequest request,
    CancellationToken ct)
    {
        var categories = await categoryRepository.GetAll(ct);
        var lookup = new CategoryLookup(categories);

        return lookup.Resolve(request.CategoryAlias!) switch
        {
            CategoryResolution.NotFound notFound => new DeleteTransactionsResponse.ValidationError([
                new ValidationErrorItem(ValidationErrorCode.Transaction.CategoryNotFound, notFound.Suggestion)
            ]),
            CategoryResolution.Found found => await DeleteTransactions(found.Category, request.Date!.Value, ct),
            _ => throw new UnreachableException()
        };
    }

    private async Task<DeleteTransactionsResponse> DeleteTransactions(
        Domain.Entities.Category category,
        DateOnly date,
        CancellationToken ct)
    {
        await transactionRepository.DeleteRange(category, date, ct);

        return new DeleteTransactionsResponse.Success(category.Name, date);
    }
}