using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.UseCases.Transaction.Create.Behaviors;

internal sealed partial class CreateTransactionLoggingBehavior(
    ILogger<CreateTransactionLoggingBehavior> logger)
    : IPipelineBehavior<CreateTransactionRequest, CreateTransactionResponse>
{
    public async Task<CreateTransactionResponse> Handle(
        CreateTransactionRequest request,
        RequestHandlerDelegate<CreateTransactionResponse> next,
        CancellationToken ct)
    {
        LogStarting(request);
        var sw = Stopwatch.StartNew();

        try
        {
            var response = await next();
            sw.Stop();

            if (response is CreateTransactionResponse.Success success)
            {
                LogSuccess(success, sw.ElapsedMilliseconds);
            }
            else if (response is CreateTransactionResponse.ValidationError validationError)
            {
                LogValidationError(validationError, sw.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            LogCriticalError(ex, sw.ElapsedMilliseconds);
            return new CreateTransactionResponse.Failure();
        }
    }
}