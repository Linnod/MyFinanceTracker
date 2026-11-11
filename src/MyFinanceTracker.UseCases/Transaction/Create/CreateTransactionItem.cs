using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.UseCases.Transaction.Create;

public record CreateTransactionItem(
    TransactionType TransactionType,
    decimal Amount,
    string CategoryId,
    DateTime Date,
    string? Note = null
);
