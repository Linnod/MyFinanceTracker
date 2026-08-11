using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyFinanceTracker.Interactions.Console;
using MyFinanceTracker.Interactions.Telegram;


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

        if (enableConsole)
        {
            services.AddConsoleInteractions();
        }

        if (enableTelegram)
        {
            services.AddTelegramInteractions(configuration);
        }

        return services;
    }
}