using FluentValidation;
using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.UseCases.Transaction.Get.Validation;

internal sealed class GetTransactionsRequestValidator : AbstractValidator<GetTransactionsRequest>
{
    public GetTransactionsRequestValidator()
    {
        RuleFor(x => x.FromDate.Year)
            .InclusiveBetween(
                FinancialRules.MinAllowedYear,
                FinancialRules.MaxAllowedYear)
            .WithErrorCode(ValidationErrorCode.Transaction.DateOutOfRange);

        RuleFor(x => x.ToDate.Year)
            .InclusiveBetween(
                FinancialRules.MinAllowedYear,
                FinancialRules.MaxAllowedYear)
            .WithErrorCode(ValidationErrorCode.Transaction.DateOutOfRange);

        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate)
            .WithErrorCode(ValidationErrorCode.Transaction.InvalidDateRange);

        When(x => x.CategoryAliases is { Count: > 0 }, () =>
        {
            RuleForEach(x => x.CategoryAliases)
                .NotEmpty()
                .WithErrorCode(ValidationErrorCode.Transaction.CategoryRequired);
        });
    }
}
