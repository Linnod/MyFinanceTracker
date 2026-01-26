namespace MyFinanceTracker.Interactions.Commands.AddTransaction.Validation;

using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Interactions.Commands.AddTransaction.Parsing;

internal sealed class AddTransactionCommandValidator : IAddTransactionCommandValidator
{
    public AddTransactionCommandValidationResult Validate(RawAddTransactionCommand raw)
    {
        if (raw.Amounts.Length == 0)
        {
            return new AddTransactionCommandValidationResult.MissingAmounts();
        }

        var (alias, error) = ProcessCategory(raw.Type, raw.CategoryAlias);
        if (error != null)
        {
            return error;
        }

        var validatedCommand = new ValidatedAddTransactionCommand(
            Type: raw.Type,
            Amounts: raw.Amounts,
            CategoryAlias: alias!,
            Date: raw.Date ?? DateOnly.FromDateTime(DateTime.Now),
            Note: raw.Note
        );

        return new AddTransactionCommandValidationResult.Success(validatedCommand);
    }

    private static (string? Alias, AddTransactionCommandValidationResult? Error) ProcessCategory(TransactionType type, string? rawAlias)
    {
        if (type == TransactionType.Expense)
        {
            if (string.IsNullOrWhiteSpace(rawAlias))
            {
                return (null, new AddTransactionCommandValidationResult.CategoryRequired(type));
            }
            
            return (rawAlias, null);
        }

        return (string.IsNullOrWhiteSpace(rawAlias) ? FinancialRules.DefaultIncomeCategoryAlias : rawAlias, null);
    }
}