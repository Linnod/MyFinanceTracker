using FluentValidation;
using MediatR;

namespace MyFinanceTracker.UseCases.Behaviors;

internal sealed class ValidationBehavior<TRequest, TResponse>(
    IValidator<TRequest> validator,
    Func<IReadOnlyList<ValidationErrorItem>, TResponse> errorFactory)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : UseCaseResponse
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var result = await validator.ValidateAsync(request, ct);
        if (!result.IsValid)
        {
            var errors = result.Errors
                .Select(e => new ValidationErrorItem(e.ErrorCode, AttemptedValue: e.AttemptedValue?.ToString()))
                .ToList();

            return errorFactory(errors);
        }

        return await next();
    }
}
