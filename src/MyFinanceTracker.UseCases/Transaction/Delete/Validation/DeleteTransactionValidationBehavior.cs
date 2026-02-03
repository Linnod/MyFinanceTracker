using FluentValidation;
using MediatR;

namespace MyFinanceTracker.UseCases.Transaction.Delete.Validation;

internal class DeleteTransactionValidationBehavior(IValidator<DeleteTransactionsRequest> validator)
    : IPipelineBehavior<DeleteTransactionsRequest, DeleteTransactionsResponse>
{
    public async Task<DeleteTransactionsResponse> Handle(
        DeleteTransactionsRequest request,
        RequestHandlerDelegate<DeleteTransactionsResponse> next,
        CancellationToken ct)
    {
        var result = await validator.ValidateAsync(request, ct);
        if (!result.IsValid)
        {
            var errors = result.Errors
                .Select(e => new ValidationErrorItem(e.PropertyName, e.ErrorMessage))
                .ToList();

            return new DeleteTransactionsResponse.ValidationError(errors);
        }

        return await next();
    }
}