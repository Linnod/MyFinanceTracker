using Microsoft.Extensions.Logging;
using MyFinanceTracker.UseCases.Behaviors;

namespace MyFinanceTracker.UseCases.Category.List.Behaviors;

internal sealed partial class ListCategoriesLoggingBehavior(
    ILogger<ListCategoriesLoggingBehavior> logger)
    : LoggingBehaviorBase<ListCategoriesRequest, ListCategoriesResponse>
{
    protected override void OnStarting(ListCategoriesRequest request) =>
        LogStarting(request);

    protected override void OnResult(ListCategoriesResponse response)
    {
        if (response is ListCategoriesResponse.Success success)
        {
            LogSuccess(success.Categories.Count);
        }
    }

    protected override void OnCriticalError(Exception ex) =>
        LogCriticalError(ex);

    protected override ListCategoriesResponse CreateFailureResponse() =>
        new ListCategoriesResponse.Failure();
}
