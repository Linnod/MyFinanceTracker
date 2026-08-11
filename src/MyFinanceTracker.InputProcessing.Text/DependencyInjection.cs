using Microsoft.Extensions.DependencyInjection;

namespace MyFinanceTracker.InputProcessing.Text;

public static class DependencyInjection
{
    public static IServiceCollection AddTextInputProcessing(
        this IServiceCollection services,
        Func<ITextInputProcessingBuilder, IProcessorConfigured> configure)
    {
        services.AddSingleton<ITextInputReceiver, TextInputReceiver>();

        var builder = new TextInputProcessingBuilder(services);
        configure(builder);

        return services;
    }
}