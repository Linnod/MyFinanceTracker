using MediatR;
using MyFinanceTracker.Common.Contracts;
using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.UseCases.Transaction.Create;

public record CreateTransactionRequest(
    TransactionType TransactionType,
    string CategoryAlias,
    decimal[] Amounts,
    DateOnly Date,
    string Note
) : IRequest<CreateTransactionResult>, ILoggableRequest
{
    string ILoggableRequest.GetLogPayload() => $"{TransactionType} | Cat: {CategoryAlias} | Total: {Amounts.Sum()}€";
}