using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.Interactions.Commands.AddTransaction.Validation;

internal abstract record AddTransactionCommandValidationResult
{
    private AddTransactionCommandValidationResult() { }

    public sealed record Success(ValidatedAddTransactionCommand Transaction) : AddTransactionCommandValidationResult;
    public sealed record MissingAmounts : AddTransactionCommandValidationResult;
    public sealed record CategoryRequired(TransactionType Type) : AddTransactionCommandValidationResult;
    public sealed record CategoryNotAllowedForIncome : AddTransactionCommandValidationResult;
    public sealed record AmbiguousTransactionType : AddTransactionCommandValidationResult;
}