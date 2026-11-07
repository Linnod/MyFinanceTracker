using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MyFinanceTracker.Common.Notifications;

namespace MyFinanceTracker.Interactions.Console;

public static class DependencyInjection
{
    public static IServiceCollection AddConsoleInteractions(this IServiceCollection services)
    {
        services.AddHostedService<ConsoleInteractionWorker>();

        services.AddTransient<INotificationHandler<OutboxSyncNotification.MessageMovedToDeadLetter>, ConsoleNotificationHandler>();
        services.AddTransient<INotificationHandler<OutboxSyncNotification.SyncFailed>, ConsoleNotificationHandler>();

        return services;
    }
}
