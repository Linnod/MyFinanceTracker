using Microsoft.Extensions.DependencyInjection;

namespace MyFinanceTracker.Interactions.Console;

public static class DependencyInjection
{
    public static IServiceCollection AddConsoleInteractions(this IServiceCollection services)
    {
        services.AddHostedService<ConsoleInteractionWorker>();

        return services;
    }
}