using Microsoft.Extensions.DependencyInjection;
using MyFinanceTracker.CommandProcessing.Text.Engine;
using MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands;
using MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.AddTransaction.Parsing;
using MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.DeleteTransaction;
using MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.DeleteTransaction.Parsing;
using MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching;
using MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.AddTransaction;
using MyFinanceTracker.CommandProcessing.Text.Engine.Interpretation;
using MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.ListCategories;

namespace MyFinanceTracker.CommandProcessing.Text;

public static class DependencyInjection
{
    public static IServiceCollection AddTextCommandProcessing(this IServiceCollection services)
    {
        return services
            .AddSingleton<ITextCommandReceiver, TextCommandReceiver>()
            .AddScoped<ITextCommandProcessor, TextCommandProcessor>()
            .AddScoped<ITextCommandInterpreter, StrictTextCommandInterpreter>()
            .AddScoped<ITextCommandDispatcher, TextCommandDispatcher>()
            .AddCommands();
    }

    private static IServiceCollection AddCommands(this IServiceCollection services)
    {
        return services
            .AddAddTransactionCommand()
            .AddDeleteTransactionCommand()
            .AddListCategoriesCommand();
    }

    private static IServiceCollection AddListCategoriesCommand(this IServiceCollection services)
    {
        return services
            .AddScoped<ICommandHandler, ListCategoriesCommandHandler>();
    }

    private static IServiceCollection AddAddTransactionCommand(this IServiceCollection services)
    {
        return services
            .AddScoped<ICommandHandler, AddTransactionCommandHandler>()
            .AddSingleton<IAddTransactionCommandParser, AddTransactionCommandRegexParser>();
    }

    private static IServiceCollection AddDeleteTransactionCommand(this IServiceCollection services)
    {
        return services
            .AddScoped<ICommandHandler, DeleteTransactionCommandHandler>()
            .AddSingleton<IDeleteTransactionCommandParser, DeleteTransactionCommandRegexParser>();
    }
}