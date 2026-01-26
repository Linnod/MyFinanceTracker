
using MyFinanceTracker.Domain.Entities;

internal record ValidatedAddTransactionCommand(
    TransactionType Type,
    decimal[] Amounts,
    string CategoryAlias,
    DateOnly Date,
    string? Note
);