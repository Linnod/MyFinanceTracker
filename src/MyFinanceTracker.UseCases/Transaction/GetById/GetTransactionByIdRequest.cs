using MediatR;

namespace MyFinanceTracker.UseCases.Transaction.GetById;

public record GetTransactionByIdRequest(Guid Id) : IRequest<GetTransactionByIdResponse>
{
    public override string ToString() => $"Id: {Id}";
}
