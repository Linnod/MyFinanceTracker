using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.UseCases.Transaction.Delete;

namespace MyFinanceTracker.UseCases.Behaviors;

internal sealed class DeleteTransactionsLoggingBehavior<TRequest, TResponse>(
    ILogger<DeleteTransactionsLoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : DeleteTransactionsRequest
    where TResponse : DeleteTransactionsResponse
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        logger.LogInformation("--> Handle");

        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        if (response is DeleteTransactionsResponse.Success)
        {
            logger.LogInformation("✅ [UseCase] Cleared ({Ms}ms)", sw.ElapsedMilliseconds);
        }
        else if (response is DeleteTransactionsResponse.Failure f)
        {
            logger.LogWarning("⚠️ [UseCase] Failed: {Msg} ({Ms}ms)", f.Message, sw.ElapsedMilliseconds);
        }

        logger.LogInformation("<-- Handle");
        
        return response;
    }
}