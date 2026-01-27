using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MyFinanceTracker.Interactions.Abstractions;
using MyFinanceTracker.Interactions.Behaviors;
using MyFinanceTracker.Interactions.Commands.AddTransaction;
using MyFinanceTracker.Interactions.Commands.AddTransaction.Parsing;
using MyFinanceTracker.Interactions.Commands.DeleteTransaction;
using MyFinanceTracker.Interactions.Commands.DeleteTransaction.Parsing;
using MyFinanceTracker.Interactions.Contracts;
using MyFinanceTracker.Interactions.Gateways;
using MyFinanceTracker.Interactions.Interpretation;

namespace MyFinanceTracker.Interactions;

public static class DependencyInjection
{
    public static IServiceCollection AddInteractions(this IServiceCollection services)
    {
        services.AddSingleton<IInteractionGateway, InteractionGateway>();
        services.AddSingleton<IInteractionInterpreter, StrictInteractionInterpreter>();
        services.AddTransactionHandling()
            .AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
                cfg.AddBehavior<IPipelineBehavior<InteractionRequest, InteractionResponse>, InteractionLoggingBehavior>();
            });

        return services;
    }

    private static IServiceCollection AddTransactionHandling(this IServiceCollection services)
    {
        return services
            .AddSingleton<IAddTransactionCommandParser, AddTransactionCommandParser>()
            .AddSingleton<IDeleteTransactionCommandParser, DeleteTransactionCommandParser>()

            .AddScoped<IInteractionHandler, AddTransactionCommandHandler>()
            .AddScoped<IInteractionHandler, DeleteTransactionCommandHandler>();
    }
}