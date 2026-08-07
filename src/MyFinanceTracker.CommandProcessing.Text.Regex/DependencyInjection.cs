using Microsoft.Extensions.DependencyInjection;
using MyFinanceTracker.CommandProcessing.Text.Regex.Dispatching;
using MyFinanceTracker.CommandProcessing.Text.Regex.Dispatching.Commands;
using MyFinanceTracker.CommandProcessing.Text.Regex.Dispatching.Commands.AddTransaction;
using MyFinanceTracker.CommandProcessing.Text.Regex.Dispatching.Commands.AddTransaction.Parsing;
using MyFinanceTracker.CommandProcessing.Text.Regex.Dispatching.Commands.DeleteTransaction;
using MyFinanceTracker.CommandProcessing.Text.Regex.Dispatching.Commands.DeleteTransaction.Parsing;
using MyFinanceTracker.CommandProcessing.Text.Regex.Dispatching.Commands.ListCategories;
using MyFinanceTracker.CommandProcessing.Text.Regex.Interpretation;

namespace MyFinanceTracker.CommandProcessing.Text.Regex;

public static class DependencyInjection
{
    public static IProcessorConfigured UseRegex(this ITextCommandProcessingBuilder builder)
    {
        builder.Services
            .AddScoped<ITextCommandProcessor, RegexTextCommandProcessor>()
            .AddScoped<ITextCommandInterpreter, StrictTextCommandInterpreter>()
            .AddSingleton<ICommandRegistry, CommandRegistry>()
            .AddScoped<ITextCommandDispatcher, TextCommandDispatcher>()
            .AddCommands();

        return new ProcessorConfigured(builder.Services);
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
            .AddScoped<ICommandHandler<ListCategoriesCommand>, ListCategoriesCommandHandler>();
    }

    private static IServiceCollection AddAddTransactionCommand(this IServiceCollection services)
    {
        return services
            .AddScoped<ICommandHandler<AddTransactionCommand>, AddTransactionCommandHandler>()
            .AddSingleton<IAddTransactionCommandPayloadParser, AddTransactionCommandPayloadRegexParser>();
    }

    private static IServiceCollection AddDeleteTransactionCommand(this IServiceCollection services)
    {
        return services
            .AddScoped<ICommandHandler<DeleteTransactionCommand>, DeleteTransactionCommandHandler>()
            .AddSingleton<IDeleteTransactionCommandPayloadParser, DeleteTransactionCommandPayloadRegexParser>();
    }
}