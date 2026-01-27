using FluentValidation;
using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.UseCases.Transaction.Delete.Validation;

internal sealed class DeleteTransactionsRequestValidator : AbstractValidator<DeleteTransactionsRequest>
{
    public DeleteTransactionsRequestValidator()
    {
        RuleFor(x => x.CategoryAlias)
            .NotEmpty()
            .WithMessage("Category alias is required for deletion.");

        RuleFor(x => x.Date)
            .NotNull()
            .WithMessage("Specific date is required to clear transactions.")
            .DependentRules(() =>
            {
                RuleFor(x => x.Date!.Value.Year)
                    .InclusiveBetween(FinancialRules.MinAllowedYear, FinancialRules.MaxAllowedYear)
                    .WithMessage($"Date must be between {FinancialRules.MinAllowedYear} and {FinancialRules.MaxAllowedYear}.");
            });
    }
}