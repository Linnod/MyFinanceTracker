using FluentValidation;
using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.UseCases.Transaction.Get.Validation;

internal sealed class GetTransactionsRequestValidator : AbstractValidator<GetTransactionsRequest>
{
    public GetTransactionsRequestValidator()
    {
        RuleFor(x => x.CategoryAlias)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCode.Transaction.CategoryRequired);

        RuleFor(x => x.Date)
            .NotNull()
            .WithErrorCode(ValidationErrorCode.Transaction.DateRequired)
            .DependentRules(() =>
            {
                RuleFor(x => x.Date!.Value.Year)
                    .InclusiveBetween(
                        FinancialRules.MinAllowedYear,
                        FinancialRules.MaxAllowedYear)
                    .WithErrorCode(ValidationErrorCode.Transaction.DateOutOfRange);
            });
    }
}
