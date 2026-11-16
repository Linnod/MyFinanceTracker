using FluentValidation;

namespace MyFinanceTracker.UseCases.Transaction.GetById.Validation;

internal sealed class GetTransactionByIdRequestValidator : AbstractValidator<GetTransactionByIdRequest>
{
    public GetTransactionByIdRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCode.Transaction.InvalidId);
    }
}
