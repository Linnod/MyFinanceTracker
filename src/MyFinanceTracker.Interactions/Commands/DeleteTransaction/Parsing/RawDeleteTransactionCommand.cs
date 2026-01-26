namespace MyFinanceTracker.Interactions.Commands.DeleteTransaction.Parsing;

internal record RawDeleteTransactionCommand(
    string CategoryAlias,
    DateOnly Date
);