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
            var errorMessage = string.Join(" ", result.Errors.Select(e => e.ErrorMessage));

            return new DeleteTransactionsResponse.ValidationError(errorMessage);
        }

        return await next();
    }
}