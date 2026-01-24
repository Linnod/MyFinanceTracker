namespace MyFinanceTracker.Domain.Entities;

public record Transaction(
    Guid Id,
    Category Category,
    decimal Amount,
    DateOnly Date,
    string Note
);