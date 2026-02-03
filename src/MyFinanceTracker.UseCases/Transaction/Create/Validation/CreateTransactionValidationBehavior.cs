using FluentValidation;
using MediatR;

namespace MyFinanceTracker.UseCases.Transaction.Create.Validation;

internal sealed class CreateTransactionValidationBehavior(IValidator<CreateTransactionRequest> validator)
    : IPipelineBehavior<CreateTransactionRequest, CreateTransactionResponse>
{
    public async Task<CreateTransactionResponse> Handle(
        CreateTransactionRequest request,
        RequestHandlerDelegate<CreateTransactionResponse> next,
        CancellationToken ct)
    {
        var result = await validator.ValidateAsync(request, ct);
        if (!result.IsValid)
        {
            var errors = result.Errors
                .Select(e => new ValidationErrorItem(e.PropertyName, e.ErrorMessage))
                .ToList();
                
            return new CreateTransactionResponse.ValidationError(errors);
        }

        return await next();
    }
}