using Microsoft.Extensions.Logging;
using MyFinanceTracker.UseCases.Behaviors;

namespace MyFinanceTracker.UseCases.Transaction.GetById.Behaviors;

internal sealed partial class GetTransactionByIdLoggingBehavior(
    ILogger<GetTransactionByIdLoggingBehavior> logger)
    : LoggingBehaviorBase<GetTransactionByIdRequest, GetTransactionByIdResponse>
{
    protected override void OnStarting(GetTransactionByIdRequest request) =>
        LogStarting(request);

    protected override void OnResult(GetTransactionByIdResponse response)
    {
        switch (response)
        {
            case GetTransactionByIdResponse.Success success:
                LogSuccess(success);
                break;

            case GetTransactionByIdResponse.ValidationError validationError:
                LogValidationError(validationError);
                break;
        }
    }

    protected override void OnCriticalError(Exception ex) =>
        LogCriticalError(ex);

    protected override GetTransactionByIdResponse CreateFailureResponse() =>
        new GetTransactionByIdResponse.Failure();
}
