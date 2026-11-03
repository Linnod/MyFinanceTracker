using FluentValidation;
using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.UseCases.Transaction.Get.Validation;

internal sealed class GetTransactionsRequestValidator : AbstractValidator<GetTransactionsRequest>
{
    public GetTransactionsRequestValidator()
    {
        RuleFor(x => x.FromDate)
            .NotNull()
            .WithErrorCode(ValidationErrorCode.Transaction.FromDateRequired)
            .DependentRules(() =>
            {
                RuleFor(x => x.FromDate!.Value.Year)
                    .InclusiveBetween(
                        FinancialRules.MinAllowedYear,
                        FinancialRules.MaxAllowedYear)
                    .WithErrorCode(ValidationErrorCode.Transaction.DateOutOfRange);
            });

        RuleFor(x => x.ToDate)
            .NotNull()
            .WithErrorCode(ValidationErrorCode.Transaction.ToDateRequired)
            .DependentRules(() =>
            {
                RuleFor(x => x.ToDate!.Value.Year)
                    .InclusiveBetween(
                        FinancialRules.MinAllowedYear,
                        FinancialRules.MaxAllowedYear)
                    .WithErrorCode(ValidationErrorCode.Transaction.DateOutOfRange);
            });

        When(x => x.FromDate.HasValue && x.ToDate.HasValue, () =>
        {
            RuleFor(x => x.ToDate!.Value)
                .GreaterThanOrEqualTo(x => x.FromDate!.Value)
                .WithErrorCode(ValidationErrorCode.Transaction.InvalidDateRange);
        });

        When(x => x.CategoryAliases is { Count: > 0 }, () =>
        {
            RuleForEach(x => x.CategoryAliases)
                .NotEmpty()
                .WithErrorCode(ValidationErrorCode.Transaction.CategoryRequired);
        });
    }
}
