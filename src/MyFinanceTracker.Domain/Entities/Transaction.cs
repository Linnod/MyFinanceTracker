namespace MyFinanceTracker.Domain.Entities;

public record Transaction(
    Guid Id,
    TransactionType Type,
    Category Category,
    decimal Amount,
    DateOnly Date,
    string? Note
);