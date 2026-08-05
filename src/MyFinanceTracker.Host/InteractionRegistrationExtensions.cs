using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyFinanceTracker.Interactions.Console;
using MyFinanceTracker.Interactions.Telegram;
using MyFinanceTracker.Interactions.Api;

namespace MyFinanceTracker.Host;

public static class InteractionRegistrationExtensions
{
    public static IServiceCollection AddConfiguredInteractions(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var section = configuration.GetSection("Interactions");
        bool enableConsole = section.GetValue<bool>("EnableConsole");
        bool enableTelegram = section.GetValue<bool>("EnableTelegram");
        bool enableApi = section.GetValue<bool>("EnableApi");

        if (enableConsole)
        {
            services.AddConsoleInteractions();
        }

        if (enableTelegram)
        {
            services.AddTelegramInteractions(configuration);
        }

        if (enableApi)
        {
            services.AddApiInteractions(configuration);
        }

        return services;
    }
}