using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;

namespace MyFinanceTracker.Host.Configuration;

internal static class ApplicationInsightsExtensions
{
    public static HostApplicationBuilder AddOptionalApplicationInsights(this HostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            builder.Services.AddApplicationInsightsTelemetryWorkerService(options =>
            {
                options.ConnectionString = connectionString;
            });

            builder.Logging.AddFilter<ApplicationInsightsLoggerProvider>("MyFinanceTracker", LogLevel.Information);
        }

        return builder;
    }
}
