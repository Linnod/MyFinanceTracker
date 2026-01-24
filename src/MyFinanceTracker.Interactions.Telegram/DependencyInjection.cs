using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace MyFinanceTracker.Interactions.Telegram;

public static class DependencyInjection
{
    public static IServiceCollection AddTelegramInteractions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<TelegramInteractionOptions>()
            .Bind(configuration.GetSection(TelegramInteractionOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient("tg_bot_client")
            .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
            {
                var opts = sp.GetRequiredService<IOptions<TelegramInteractionOptions>>().Value;

                return new TelegramBotClient(opts.Token, httpClient);
            });

        services.AddHostedService<TelegramInteractionWorker>();

        return services;
    }
}

