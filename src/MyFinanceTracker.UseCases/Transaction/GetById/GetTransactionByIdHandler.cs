using MediatR;
using MyFinanceTracker.Domain.Repositories;

namespace MyFinanceTracker.UseCases.Transaction.GetById;

internal sealed class GetTransactionByIdHandler(
    ITransactionRepository transactionRepository)
    : IRequestHandler<GetTransactionByIdRequest, GetTransactionByIdResponse>
{
    public async Task<GetTransactionByIdResponse> Handle(
        GetTransactionByIdRequest request,
        CancellationToken ct)
    {
        var transaction = await transactionRepository.GetById(request.Id, ct);
        if (transaction is null)
        {
            return new GetTransactionByIdResponse.ValidationError([
                new ValidationErrorItem(ValidationErrorCode.Transaction.TransactionNotFound, request.Id.ToString())
            ]);
        }

        return new GetTransactionByIdResponse.Success(transaction);
    }
}
