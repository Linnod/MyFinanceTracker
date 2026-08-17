using FluentValidation;
using MediatR;

namespace MyFinanceTracker.UseCases.Transaction.Get.Validation;

internal sealed class GetTransactionsValidationBehavior(
    IValidator<GetTransactionsRequest> validator)
    : IPipelineBehavior<GetTransactionsRequest, GetTransactionsResponse>
{
    public async Task<GetTransactionsResponse> Handle(
        GetTransactionsRequest request,
        RequestHandlerDelegate<GetTransactionsResponse> next,
        CancellationToken ct)
    {
        var result = await validator.ValidateAsync(request, ct);
        if (!result.IsValid)
        {
            var errors = result.Errors
                .Select(e => new ValidationErrorItem(e.ErrorCode))
                .ToList();

            return new GetTransactionsResponse.ValidationError(errors);
        }

        return await next();
    }
}