using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.UseCases.Transaction.Create;
using MyFinanceTracker.UseCases.Transaction.Delete;

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

        var elapsedMs = sw.ElapsedMilliseconds;

        if (response is CreateTransactionResult ctr)
        {
            switch (ctr)
            {
                case CreateTransactionResult.Success:
                    logger.LogInformation("✅ [UseCase] Transaction saved ({Ms}ms)", elapsedMs);
                    break;
                case CreateTransactionResult.Failure f:
                    logger.LogWarning("⚠️ [UseCase] Business error: {Msg} ({Ms}ms)", f.Message, elapsedMs);
                    break;
            }
        }

        if (response is DeleteTransactionsResponse dtr)
        {
            switch (dtr)
            {
                case DeleteTransactionsResponse.Success:
                    logger.LogInformation("✅ [UseCase] Records cleared and reset to zero for specified category and date ({Ms}ms)", elapsedMs);
                    break;

                case DeleteTransactionsResponse.Failure f:
                    logger.LogWarning("⚠️ [UseCase] Deletion failed: {Msg} ({Ms}ms)", f.Message, elapsedMs);
                    break;
            }
        }

        return response;
    }
}