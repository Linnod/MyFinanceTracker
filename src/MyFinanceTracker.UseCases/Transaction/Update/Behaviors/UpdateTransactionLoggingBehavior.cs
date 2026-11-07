using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.UseCases.Transaction.Update.Behaviors;

internal sealed partial class UpdateTransactionLoggingBehavior(
    ILogger<UpdateTransactionLoggingBehavior> logger)
    : IPipelineBehavior<UpdateTransactionRequest, UpdateTransactionResponse>
{
    public async Task<UpdateTransactionResponse> Handle(
        UpdateTransactionRequest request,
        RequestHandlerDelegate<UpdateTransactionResponse> next,
        CancellationToken ct)
    {
        LogStarting(request);
        var sw = Stopwatch.StartNew();

        try
        {
            var response = await next();
            sw.Stop();

            switch (response)
            {
                case UpdateTransactionResponse.Success s:
                    LogSuccess(s, sw.ElapsedMilliseconds);
                    break;

                case UpdateTransactionResponse.ValidationError v:
                    LogValidationError(v, sw.ElapsedMilliseconds);
                    break;

                case UpdateTransactionResponse.Failure:
                    LogFailure(sw.ElapsedMilliseconds);
                    break;
            }

            return response;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            LogCriticalError(ex, sw.ElapsedMilliseconds);
            return new UpdateTransactionResponse.Failure();
        }
    }
}
