using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MyFinanceTracker.Interactions.Behaviors;
using MyFinanceTracker.Interactions.Parsing;
using MyFinanceTracker.Interactions.Parsing.Parser;
using MyFinanceTracker.Interactions.Parsing.Validation;

namespace MyFinanceTracker.Interactions;

public static class DependencyInjection
{
    public static IServiceCollection AddInteractions(this IServiceCollection services)
    {
        services.AddParsing()
            .AddMediatR(cfg => 
            {
                cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            });

        return services;
    }

    private static IServiceCollection AddParsing(this IServiceCollection services)
    {
        return services.AddSingleton<IFinancialOperationParser, FinancialOperationParser>()
            .AddSingleton<ParsingService>()
            .AddSingleton<IRawFinancialOperationValidator, RawFinancialOperationValidator>();
    }
}