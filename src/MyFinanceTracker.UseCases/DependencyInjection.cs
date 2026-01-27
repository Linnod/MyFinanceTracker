using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MyFinanceTracker.UseCases.Behaviors;
using MyFinanceTracker.UseCases.Transaction.Create;
using MyFinanceTracker.UseCases.Transaction.Create.Validation;
using MyFinanceTracker.UseCases.Transaction.Delete;
using MyFinanceTracker.UseCases.Transaction.Delete.Validation;

namespace MyFinanceTracker.UseCases;

public static class DependencyInjection
{
    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CreateTransactionLoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DeleteTransactionsLoggingBehavior<,>));
        services.AddTransactionCreateValidation();
        services.AddTransactionDeleteValidation();

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

    private static IServiceCollection AddTransactionDeleteValidation(this IServiceCollection services)
    {
        services.AddScoped<IValidator<DeleteTransactionsRequest>, DeleteTransactionsRequestValidator>();

        services.AddTransient<
            IPipelineBehavior<DeleteTransactionsRequest, DeleteTransactionsResponse>,
            DeleteTransactionValidationBehavior>();

        return services;
    }
}