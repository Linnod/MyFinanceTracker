using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.UseCases.Transaction.Delete.Behaviors;

internal sealed partial class DeleteTransactionsLoggingBehavior(
    ILogger<DeleteTransactionsLoggingBehavior> logger)
    : IPipelineBehavior<DeleteTransactionsRequest, DeleteTransactionsResponse>
{
    public async Task<DeleteTransactionsResponse> Handle(
        DeleteTransactionsRequest request,
        RequestHandlerDelegate<DeleteTransactionsResponse> next,
        CancellationToken ct)
    {
        LogStarting(request);
        var sw = Stopwatch.StartNew();

        try
        {
            var response = await next();
            sw.Stop();

            if (response is DeleteTransactionsResponse.Success success)
            {
                LogSuccess(success, sw.ElapsedMilliseconds);
            }
            else if (response is DeleteTransactionsResponse.ValidationError validationError)
            {
                LogValidationError(validationError, sw.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            LogCriticalError(ex, sw.ElapsedMilliseconds);
            return new DeleteTransactionsResponse.Failure();
        }
    }
}