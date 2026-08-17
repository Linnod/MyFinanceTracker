using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.UseCases.Transaction.Get.Behaviors;

internal sealed partial class GetTransactionsLoggingBehavior(
    ILogger<GetTransactionsLoggingBehavior> logger)
    : IPipelineBehavior<GetTransactionsRequest, GetTransactionsResponse>
{
    public async Task<GetTransactionsResponse> Handle(
        GetTransactionsRequest request,
        RequestHandlerDelegate<GetTransactionsResponse> next,
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
                case GetTransactionsResponse.Success success:
                    LogSuccess(success, sw.ElapsedMilliseconds);
                    break;

                case GetTransactionsResponse.ValidationError validationError:
                    LogValidationError(validationError, sw.ElapsedMilliseconds);
                    break;

                case GetTransactionsResponse.Failure:
                    LogFailure(sw.ElapsedMilliseconds);
                    break;
            }

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            LogCriticalError(ex, sw.ElapsedMilliseconds);
            return new GetTransactionsResponse.Failure();
        }
    }
}