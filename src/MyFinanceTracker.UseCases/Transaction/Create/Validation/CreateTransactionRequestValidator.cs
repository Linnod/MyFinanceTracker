using FluentValidation;
using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.UseCases.Transaction.Create.Validation;

internal sealed class CreateTransactionsRequestValidator : AbstractValidator<CreateTransactionsRequest>
{
    public CreateTransactionsRequestValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCode.Common.Required);

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Amount)
                .GreaterThan(0)
                .WithErrorCode(ValidationErrorCode.Common.MustBePositive);

            item.When(i => i.Date.HasValue, () =>
            {
                item.RuleFor(i => i.Date!.Value.Year)
                    .InclusiveBetween(FinancialRules.MinAllowedYear, FinancialRules.MaxAllowedYear)
                    .WithErrorCode(ValidationErrorCode.Common.DateOutOfRange);
            });
        });
    }
}