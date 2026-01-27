using FluentValidation;
using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.UseCases.Transaction.Create.Validation;

internal sealed class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequest>
{
    public CreateTransactionRequestValidator()
    {
        RuleFor(x => x.Amounts)
            .NotEmpty().WithMessage("At least one amount is required.")
            .Must(a => a.All(v => v > 0)).WithMessage("All amounts must be greater than zero.");

        RuleFor(x => x.Date)
            .Must(d => !d.HasValue || (d.Value.Year >= FinancialRules.MinAllowedYear && d.Value.Year <= FinancialRules.MaxAllowedYear))
            .WithMessage($"Date must be between {FinancialRules.MinAllowedYear} and {FinancialRules.MaxAllowedYear}.");
    }
}