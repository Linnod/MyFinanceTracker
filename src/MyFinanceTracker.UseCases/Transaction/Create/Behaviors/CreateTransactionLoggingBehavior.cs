using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.UseCases.Transaction.Create;

namespace MyFinanceTracker.UseCases.Behaviors;

internal sealed class CreateTransactionLoggingBehavior<TRequest, TResponse>(
    ILogger<CreateTransactionLoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : CreateTransactionRequest
    where TResponse : CreateTransactionResponse
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        logger.LogInformation("--> Handle");

        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        if (response is CreateTransactionResponse.Success)
        {
            logger.LogInformation("✅ [UseCase] Saved ({Ms}ms)", sw.ElapsedMilliseconds);
        }
        else if (response is CreateTransactionResponse.Failure f)
        {
            logger.LogWarning("⚠️ [UseCase] Business error: {Msg} ({Ms}ms)", f.Message, sw.ElapsedMilliseconds);
        }

        logger.LogInformation("<-- Handle");
        
        return response;
    }
}