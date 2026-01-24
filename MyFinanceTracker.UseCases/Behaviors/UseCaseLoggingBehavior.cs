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
            switch (ctr)
            {
                case CreateTransactionResult.Success:
                    logger.LogInformation("✅ [UseCase] Transaction saved ({Ms}ms)", sw.ElapsedMilliseconds);
                    break;

                case CreateTransactionResult.Failure f:
                    logger.LogWarning("⚠️ [UseCase] Business error: {Msg} ({Ms}ms)", f.Message, sw.ElapsedMilliseconds);
                    break;
            }
        }

        return response;
    }
}