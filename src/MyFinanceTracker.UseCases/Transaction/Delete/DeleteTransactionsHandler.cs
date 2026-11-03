using System.Diagnostics;
using MediatR;
using MyFinanceTracker.Domain.Repositories;

namespace MyFinanceTracker.UseCases.Transaction.Delete;

internal sealed class DeleteTransactionsHandler(
    ITransactionRepository transactionRepository)
    : IRequestHandler<DeleteTransactionsRequest, DeleteTransactionsResponse>
{
    public async Task<DeleteTransactionsResponse> Handle(
        DeleteTransactionsRequest request,
        CancellationToken ct)
    {
        var distinctIds = request.Ids.Distinct().ToList();
        var result = await transactionRepository.DeleteRange(distinctIds, ct);

        return result switch
        {
            DeleteTransactionsResult.Success s => new DeleteTransactionsResponse.Success(s.DeletedCount),

            DeleteTransactionsResult.NotFound nf => new DeleteTransactionsResponse.ValidationError(
                [.. nf.MissingIds
                    .Select(id => new ValidationErrorItem(
                        ValidationErrorCode.Transaction.TransactionNotFound,
                        AttemptedValue: id.ToString()))]),

            _ => throw new UnreachableException()
        };
    }
}
