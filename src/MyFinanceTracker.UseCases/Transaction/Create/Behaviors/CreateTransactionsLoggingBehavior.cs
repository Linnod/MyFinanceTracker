using Microsoft.Extensions.Logging;
using MyFinanceTracker.UseCases.Behaviors;

namespace MyFinanceTracker.UseCases.Transaction.Create.Behaviors;

internal sealed partial class CreateTransactionsLoggingBehavior(
    ILogger<CreateTransactionsLoggingBehavior> logger)
    : LoggingBehaviorBase<CreateTransactionsRequest, CreateTransactionsResponse>
{
    protected override void OnStarting(CreateTransactionsRequest request) =>
        LogStarting(request);

    protected override void OnResult(CreateTransactionsResponse response)
    {
        switch (response)
        {
            case CreateTransactionsResponse.Success s:
                LogSuccess(s);
                break;

            case CreateTransactionsResponse.ValidationError v:
                LogValidationError(v);
                break;
        }
    }

    protected override void OnCriticalError(Exception ex) =>
        LogCriticalError(ex);

    protected override CreateTransactionsResponse CreateFailureResponse() =>
        new CreateTransactionsResponse.Failure();
}
