using FluentValidation;

namespace MyFinanceTracker.UseCases.Transaction.Delete.Validation;

internal sealed class DeleteTransactionsRequestValidator : AbstractValidator<DeleteTransactionsRequest>
{
    public DeleteTransactionsRequestValidator()
    {
        RuleFor(x => x.Ids)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCode.Transaction.IdRequired)
            .DependentRules(() =>
            {
                RuleForEach(x => x.Ids)
                    .NotEmpty()
                    .WithErrorCode(ValidationErrorCode.Transaction.InvalidId);
            });
    }
}
