using Microsoft.Extensions.DependencyInjection;
using MyFinanceTracker.InputProcessing.Text.Regex.Dispatching;
using MyFinanceTracker.InputProcessing.Text.Regex.Dispatching.Commands;
using MyFinanceTracker.InputProcessing.Text.Regex.Dispatching.Commands.AddTransaction;
using MyFinanceTracker.InputProcessing.Text.Regex.Dispatching.Commands.AddTransaction.Parsing;
using MyFinanceTracker.InputProcessing.Text.Regex.Dispatching.Commands.DeleteTransaction;
using MyFinanceTracker.InputProcessing.Text.Regex.Dispatching.Commands.DeleteTransaction.Parsing;
using MyFinanceTracker.InputProcessing.Text.Regex.Dispatching.Commands.ListCategories;
using MyFinanceTracker.InputProcessing.Text.Regex.Interpretation;

namespace MyFinanceTracker.InputProcessing.Text.Regex;

public static class DependencyInjection
{
    public static IProcessorConfigured UseRegex(this ITextInputProcessingBuilder builder)
    {
        builder.Services
            .AddScoped<ITextInputProcessor, RegexTextInputProcessor>()
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