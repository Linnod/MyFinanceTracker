using MediatR;

namespace MyFinanceTracker.UseCases.Behaviors;

internal abstract class LoggingBehaviorBase<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : UseCaseResponse
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        OnStarting(request);

        try
        {
            var response = await next();
            OnResult(response);
            return response;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            OnCriticalError(ex);
            return CreateFailureResponse();
        }
    }

    protected abstract void OnStarting(TRequest request);

    protected abstract void OnResult(TResponse response);

    protected abstract void OnCriticalError(Exception ex);

    protected abstract TResponse CreateFailureResponse();
}
