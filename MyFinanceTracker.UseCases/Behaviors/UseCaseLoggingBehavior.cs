using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.UseCases.Transaction.Create;

namespace MyFinanceTracker.UseCases.Behaviors;

internal sealed class UseCaseLoggingBehavior<TRequest, TResponse>(
    ILogger<UseCaseLoggingBehavior<TRequest, TResponse>> logger) 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        if (response is CreateTransactionResult ctr)
        {
            ctr.Switch(
                onSuccess: () => logger.LogInformation("✅ [UseCase] Transaction saved ({Ms}ms)", sw.ElapsedMilliseconds),
                onFailure: msg => logger.LogWarning("⚠️ [UseCase] Business error: {Msg} ({Ms}ms)", msg, sw.ElapsedMilliseconds)
            );
        }

        return response;
    }
}