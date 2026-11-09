using Microsoft.Extensions.Logging;
using MyFinanceTracker.UseCases.Behaviors;

namespace MyFinanceTracker.UseCases.Transaction.Get.Behaviors;

internal sealed partial class GetTransactionsLoggingBehavior(
    ILogger<GetTransactionsLoggingBehavior> logger)
    : LoggingBehaviorBase<GetTransactionsRequest, GetTransactionsResponse>
{
    protected override void OnStarting(GetTransactionsRequest request) =>
        LogStarting(request);

    protected override void OnResult(GetTransactionsResponse response)
    {
        switch (response)
        {
            case GetTransactionsResponse.Success success:
                LogSuccess(success);
                break;

            case GetTransactionsResponse.ValidationError validationError:
                LogValidationError(validationError);
                break;
        }
    }

    protected override void OnCriticalError(Exception ex) =>
        LogCriticalError(ex);

    protected override GetTransactionsResponse CreateFailureResponse() =>
        new GetTransactionsResponse.Failure();
}
