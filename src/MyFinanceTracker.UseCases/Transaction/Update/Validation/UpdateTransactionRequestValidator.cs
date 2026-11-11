using FluentValidation;
using FluentValidation.Results;
using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.UseCases.Transaction.Update.Validation;

internal sealed class UpdateTransactionRequestValidator : AbstractValidator<UpdateTransactionRequest>
{
    public UpdateTransactionRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCode.Transaction.InvalidId);

        RuleFor(x => x)
            .Custom((request, context) =>
            {
                if (!HasAnyFieldToUpdate(request))
                {
                    context.AddFailure(new ValidationFailure
                    {
                        PropertyName = string.Empty,
                        ErrorCode = ValidationErrorCode.Transaction.NothingToUpdate,
                        AttemptedValue = null
                    });
                }
            });

        When(x => x.Amount.HasValue, () =>
        {
            RuleFor(x => x.Amount!.Value)
                .GreaterThan(0)
                .WithErrorCode(ValidationErrorCode.Transaction.AmountMustBePositive);
        });

        When(x => x.Date.HasValue, () =>
        {
            RuleFor(x => x.Date!.Value.Year)
                .InclusiveBetween(FinancialRules.MinAllowedYear, FinancialRules.MaxAllowedYear)
                .WithErrorCode(ValidationErrorCode.Transaction.DateOutOfRange);
        });
    }

    private static bool HasAnyFieldToUpdate(UpdateTransactionRequest request)
    {
        return request.Type is not null
            || request.Amount is not null
            || !string.IsNullOrWhiteSpace(request.CategoryId)
            || request.Date is not null
            || !string.IsNullOrWhiteSpace(request.Note)
            || request.ClearNote;
    }
}
