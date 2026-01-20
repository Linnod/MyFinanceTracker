using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.Interactions.Parsing.Models;

internal record RawFinancialOperation(
    FinancialOperationType? Type,
    string? CategoryAlias,
    decimal[] Amounts,
    DateOnly? Date,
    string Notes
);