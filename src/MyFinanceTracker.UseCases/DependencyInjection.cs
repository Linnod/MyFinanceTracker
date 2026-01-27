using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MyFinanceTracker.UseCases.Behaviors;
using MyFinanceTracker.UseCases.Transaction.Create;
using MyFinanceTracker.UseCases.Transaction.Create.Validation;

namespace MyFinanceTracker.UseCases;

public static class DependencyInjection
{
    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly)
               .AddOpenBehavior(typeof(UseCaseLoggingBehavior<,>));
        });

        services.AddTransactionCreateValidation();

        return services;
    }

    private static IServiceCollection AddTransactionCreateValidation(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateTransactionRequest>, CreateTransactionRequestValidator>();

        services.AddTransient<
            IPipelineBehavior<CreateTransactionRequest, CreateTransactionResponse>,
            CreateTransactionValidationBehavior>();

        return services;
    }
}