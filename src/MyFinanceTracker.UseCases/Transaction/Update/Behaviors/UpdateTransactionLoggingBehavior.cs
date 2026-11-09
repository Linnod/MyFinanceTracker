using Microsoft.Extensions.Logging;
using MyFinanceTracker.UseCases.Behaviors;

namespace MyFinanceTracker.UseCases.Transaction.Update.Behaviors;

internal sealed partial class UpdateTransactionLoggingBehavior(
    ILogger<UpdateTransactionLoggingBehavior> logger)
    : LoggingBehaviorBase<UpdateTransactionRequest, UpdateTransactionResponse>
{
    protected override void OnStarting(UpdateTransactionRequest request) =>
        LogStarting(request);

    protected override void OnResult(UpdateTransactionResponse response)
    {
        switch (response)
        {
            case UpdateTransactionResponse.Success s:
                LogSuccess(s);
                break;

            case UpdateTransactionResponse.ValidationError v:
                LogValidationError(v);
                break;
        }
    }

    protected override void OnCriticalError(Exception ex) =>
        LogCriticalError(ex);

    protected override UpdateTransactionResponse CreateFailureResponse() =>
        new UpdateTransactionResponse.Failure();
}
