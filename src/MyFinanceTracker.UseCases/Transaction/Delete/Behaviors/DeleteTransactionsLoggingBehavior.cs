using Microsoft.Extensions.Logging;
using MyFinanceTracker.UseCases.Behaviors;

namespace MyFinanceTracker.UseCases.Transaction.Delete.Behaviors;

internal sealed partial class DeleteTransactionsLoggingBehavior(
    ILogger<DeleteTransactionsLoggingBehavior> logger)
    : LoggingBehaviorBase<DeleteTransactionsRequest, DeleteTransactionsResponse>
{
    protected override void OnStarting(DeleteTransactionsRequest request) =>
        LogStarting(request);

    protected override void OnResult(DeleteTransactionsResponse response)
    {
        switch (response)
        {
            case DeleteTransactionsResponse.Success success:
                LogSuccess(success);
                break;

            case DeleteTransactionsResponse.ValidationError validationError:
                LogValidationError(validationError);
                break;
        }
    }

    protected override void OnCriticalError(Exception ex) =>
        LogCriticalError(ex);

    protected override DeleteTransactionsResponse CreateFailureResponse() =>
        new DeleteTransactionsResponse.Failure();
}
