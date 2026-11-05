using Microsoft.Extensions.DependencyInjection;

namespace MyFinanceTracker.InputProcessing;

public static class DependencyInjection
{
    public static IServiceCollection AddInputProcessing(this IServiceCollection services)
    {
        return services.AddSingleton<IInputReceiver, InputReceiver>();
    }
}
