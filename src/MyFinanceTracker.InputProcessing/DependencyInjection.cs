using Microsoft.Extensions.DependencyInjection;

namespace MyFinanceTracker.InputProcessing;

public static class DependencyInjection
{
    public static IServiceCollection AddInputProcessing(
        this IServiceCollection services,
        Func<ITextInputProcessingBuilder, IProcessorConfigured> configure)
    {
        services.AddSingleton<IInputReceiver, InputReceiver>();

        var builder = new TextInputProcessingBuilder(services);
        configure(builder);

        return services;
    }
}