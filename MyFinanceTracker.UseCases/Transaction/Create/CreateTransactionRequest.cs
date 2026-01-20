using MediatR;
using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.UseCases.Transaction.Create;

public record CreateTransactionRequest(
    FinancialOperationType Type,
    string CategoryAlias,
    decimal[] Amounts,
    DateOnly Date,
    string Note
) : IRequest<CreateTransactionResult>;
