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
            var errorMessage = string.Join(" ", result.Errors.Select(e => e.ErrorMessage));

            return new CreateTransactionResponse.ValidationError(errorMessage);
        }

        return await next();
    }
}