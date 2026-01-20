using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace MyFinanceTracker.UseCases;

public static class DependencyInjection
{
    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        services.AddValidatorsFromAssembly(assembly);
        services.AddMediatR(cfg => 
        { 
            cfg.RegisterServicesFromAssembly(assembly); 
        });

        return services;
    }
}