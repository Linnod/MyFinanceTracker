using FluentValidation;
using MediatR;

namespace MyFinanceTracker.UseCases.Transaction.Create.Validation;

internal sealed class CreateTransactionsValidationBehavior(IValidator<CreateTransactionsRequest> validator)
    : IPipelineBehavior<CreateTransactionsRequest, CreateTransactionsResponse>
{
    public async Task<CreateTransactionsResponse> Handle(
        CreateTransactionsRequest request,
        RequestHandlerDelegate<CreateTransactionsResponse> next,
        CancellationToken ct)
    {
        var result = await validator.ValidateAsync(request, ct);
        if (!result.IsValid)
        {
            var errors = result.Errors
                .Select(e => new ValidationErrorItem(e.ErrorCode))
                .ToList();

            return new CreateTransactionsResponse.ValidationError(errors);
        }

        return await next();
    }
}