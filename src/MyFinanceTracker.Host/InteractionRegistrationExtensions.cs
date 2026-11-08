using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyFinanceTracker.Interactions.Telegram;

namespace MyFinanceTracker.Host;

public static class InteractionRegistrationExtensions
{
    public static IServiceCollection AddConfiguredInteractions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection("Interactions");
        bool enableTelegram = section.GetValue<bool>("EnableTelegram");

        if (enableTelegram)
        {
            services.AddTelegramInteractions(configuration);
        }

        return services;
    }
}
