using Microsoft.Extensions.DependencyInjection;

namespace MyFinanceTracker.CommandProcessing.Text;

public static class DependencyInjection
{
    public static IServiceCollection AddTextCommandProcessing(
        this IServiceCollection services,
        Func<ITextCommandProcessingBuilder, IProcessorConfigured> configure)
    {
        services.AddSingleton<ITextCommandReceiver, TextCommandReceiver>();

        var builder = new TextCommandProcessingBuilder(services);
        configure(builder);

        return services;
    }
}