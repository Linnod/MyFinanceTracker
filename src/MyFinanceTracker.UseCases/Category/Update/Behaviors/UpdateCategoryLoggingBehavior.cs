using Microsoft.Extensions.Logging;
using MyFinanceTracker.UseCases.Behaviors;

namespace MyFinanceTracker.UseCases.Category.Update.Behaviors;

internal sealed partial class UpdateCategoryLoggingBehavior(
    ILogger<UpdateCategoryLoggingBehavior> logger)
    : LoggingBehaviorBase<UpdateCategoryRequest, UpdateCategoryResponse>
{
    protected override void OnStarting(UpdateCategoryRequest request) =>
        LogStarting(request);

    protected override void OnResult(UpdateCategoryResponse response)
    {
        switch (response)
        {
            case UpdateCategoryResponse.Success s:
                LogSuccess(s);
                break;

            case UpdateCategoryResponse.ValidationError v:
                LogValidationError(v);
                break;
        }
    }

    protected override void OnCriticalError(Exception ex) =>
        LogCriticalError(ex);

    protected override UpdateCategoryResponse CreateFailureResponse() =>
        new UpdateCategoryResponse.Failure();
}
