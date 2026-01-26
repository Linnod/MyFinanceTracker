namespace MyFinanceTracker.Interactions.Commands.AddTransaction.Validation;

using MyFinanceTracker.Interactions.Commands.AddTransaction.Parsing;

internal interface IAddTransactionCommandValidator
{
    AddTransactionCommandValidationResult Validate(RawAddTransactionCommand rawCommand);
}