using FluentValidation;
using MediatR;

namespace MyFinanceTracker.UseCases.Transaction.Update.Validation;

internal sealed class UpdateTransactionValidationBehavior(IValidator<UpdateTransactionRequest> validator)
    : IPipelineBehavior<UpdateTransactionRequest, UpdateTransactionResponse>
{
    public async Task<UpdateTransactionResponse> Handle(
        UpdateTransactionRequest request,
        RequestHandlerDelegate<UpdateTransactionResponse> next,
        CancellationToken ct)
    {
        var result = await validator.ValidateAsync(request, ct);
        if (!result.IsValid)
        {
            var errors = result.Errors
                .Select(e => new ValidationErrorItem(e.ErrorCode, AttemptedValue: e.AttemptedValue?.ToString()))
                .ToList();

            return new UpdateTransactionResponse.ValidationError(errors);
        }

        return await next();
    }
}
