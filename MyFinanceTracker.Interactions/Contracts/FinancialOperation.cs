using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.Interactions.Contracts;

public record FinancialOperation(
    FinancialOperationType Type,
    string CategoryAlias,
    decimal[] Amounts,
    DateOnly Date,
    string Notes
);