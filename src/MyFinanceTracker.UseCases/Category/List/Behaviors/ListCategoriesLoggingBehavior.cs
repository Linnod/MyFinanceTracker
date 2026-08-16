using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.UseCases.Category.List.Behaviors;

internal sealed partial class ListCategoriesLoggingBehavior(
    ILogger<ListCategoriesLoggingBehavior> logger)
    : IPipelineBehavior<ListCategoriesRequest, ListCategoriesResponse>
{
    public async Task<ListCategoriesResponse> Handle(
        ListCategoriesRequest request,
        RequestHandlerDelegate<ListCategoriesResponse> next,
        CancellationToken ct)
    {
        LogStarting(request);
        var sw = Stopwatch.StartNew();

        try
        {
            var response = await next();
            sw.Stop();

            if (response is ListCategoriesResponse.Success success)
            {
                LogSuccess(success.Categories.Count, sw.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            LogCriticalError(ex, sw.ElapsedMilliseconds);

            return new ListCategoriesResponse.Failure();
        }
    }
}