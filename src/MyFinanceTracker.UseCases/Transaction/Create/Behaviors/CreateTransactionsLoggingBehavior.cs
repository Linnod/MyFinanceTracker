using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.UseCases.Transaction.Create.Behaviors;

internal sealed partial class CreateTransactionsLoggingBehavior(
    ILogger<CreateTransactionsLoggingBehavior> logger)
    : IPipelineBehavior<CreateTransactionsRequest, CreateTransactionsResponse>
{
    public async Task<CreateTransactionsResponse> Handle(
        CreateTransactionsRequest request,
        RequestHandlerDelegate<CreateTransactionsResponse> next,
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
            case CreateTransactionsResponse.Success s:
                LogSuccess(s, sw.ElapsedMilliseconds);
                break;

            case CreateTransactionsResponse.ValidationError v:
                LogValidationError(v, sw.ElapsedMilliseconds);
                break;

            case CreateTransactionsResponse.Failure:
                LogFailure(sw.ElapsedMilliseconds);
                break;
        }

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            LogCriticalError(ex, sw.ElapsedMilliseconds);
            return new CreateTransactionsResponse.Failure();
        }
    }
}