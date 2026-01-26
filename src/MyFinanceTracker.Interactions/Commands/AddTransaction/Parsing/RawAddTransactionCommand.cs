using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.Interactions.Commands.AddTransaction.Parsing;

internal record RawAddTransactionCommand(
    TransactionType Type,
    decimal[] Amounts,
    string? CategoryAlias = null,
    DateOnly? Date = null,
    string? Note = null
);