using FluentValidation;
using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.UseCases.Transaction.Create.Validation;

internal sealed class CreateTransactionsRequestValidator : AbstractValidator<CreateTransactionsRequest>
{
    public CreateTransactionsRequestValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCode.Transaction.AmountRequired);

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Amount)
                .GreaterThan(0)
                .WithErrorCode(ValidationErrorCode.Transaction.AmountMustBePositive);

            item.When(i => i.Date.HasValue, () =>
            {
                item.RuleFor(i => i.Date!.Value.Year)
                    .InclusiveBetween(FinancialRules.MinAllowedYear, FinancialRules.MaxAllowedYear)
                    .WithErrorCode(ValidationErrorCode.Transaction.DateOutOfRange);
            });
        });
    }
}
