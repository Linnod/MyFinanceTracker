using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MyFinanceTracker.UseCases.Behaviors;

namespace MyFinanceTracker.UseCases;

public static class DependencyInjection
{
    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        services.AddValidatorsFromAssembly(assembly);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly)
                .AddOpenBehavior(typeof(UseCaseLoggingBehavior<,>));
        });

        return services;
    }
}