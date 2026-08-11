using FluentValidation;
using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.UseCases.Transaction.Delete.Validation;

internal sealed class DeleteTransactionsRequestValidator : AbstractValidator<DeleteTransactionsRequest>
{
    public DeleteTransactionsRequestValidator()
    {
        RuleFor(x => x.CategoryAlias)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCode.Common.Required);

        RuleFor(x => x.Date)
            .NotNull()
            .WithErrorCode(ValidationErrorCode.Common.Required)
            .DependentRules(() =>
            {
                RuleFor(x => x.Date!.Value.Year)
                    .InclusiveBetween(FinancialRules.MinAllowedYear, FinancialRules.MaxAllowedYear)
                    .WithErrorCode(ValidationErrorCode.Common.DateOutOfRange);
            });
    }
}