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
        var deletedCount = await transactionRepository.DeleteRange(distinctIds, ct);

        return new DeleteTransactionsResponse.Success(deletedCount);
    }
}
