namespace MyFinanceTracker.Domain.Entities;

public record Transaction(
    Category Category,
    decimal Amount,
    DateOnly Date,
    string Note
);