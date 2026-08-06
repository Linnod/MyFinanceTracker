using MyFinanceTracker.Interactions.Api.Extensions;

namespace MyFinanceTracker.Interactions.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApiInteractions(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddOptions<ApiInteractionOptions>()
            .Bind(configuration.GetSection(ApiInteractionOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHostedService<ApiInteractionWorker>();

        return services;
    }
}