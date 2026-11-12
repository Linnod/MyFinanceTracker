using Microsoft.Extensions.Logging;
using MyFinanceTracker.UseCases.Behaviors;

namespace MyFinanceTracker.UseCases.Category.Create.Behaviors;

internal sealed partial class CreateCategoryLoggingBehavior(
    ILogger<CreateCategoryLoggingBehavior> logger)
    : LoggingBehaviorBase<CreateCategoryRequest, CreateCategoryResponse>
{
    protected override void OnStarting(CreateCategoryRequest request) =>
        LogStarting(request);

    protected override void OnResult(CreateCategoryResponse response)
    {
        switch (response)
        {
            case CreateCategoryResponse.Success s:
                LogSuccess(s);
                break;

            case CreateCategoryResponse.ValidationError v:
                LogValidationError(v);
                break;
        }
    }

    protected override void OnCriticalError(Exception ex) =>
        LogCriticalError(ex);

    protected override CreateCategoryResponse CreateFailureResponse() =>
        new CreateCategoryResponse.Failure();
}
