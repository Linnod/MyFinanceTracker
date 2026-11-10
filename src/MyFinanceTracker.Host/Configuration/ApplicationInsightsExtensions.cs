using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MyFinanceTracker.Host.Configuration;

internal static class ApplicationInsightsExtensions
{
    public static IServiceCollection AddOptionalApplicationInsights(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddApplicationInsightsTelemetryWorkerService(options =>
            {
                options.ConnectionString = connectionString;
            });
        }

        return services;
    }
}
