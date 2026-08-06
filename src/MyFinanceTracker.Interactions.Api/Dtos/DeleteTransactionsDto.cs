namespace MyFinanceTracker.Interactions.Api.Dtos;

public record DeleteTransactionsDto(
    string CategoryAlias,
    DateOnly Date
);