using Microsoft.Extensions.DependencyInjection;
using MyFinanceTracker.InputProcessing.Text.Structured.Dispatching;
using MyFinanceTracker.InputProcessing.Text.Structured.Dispatching.Commands;
using MyFinanceTracker.InputProcessing.Text.Structured.Dispatching.Commands.AddTransaction;
using MyFinanceTracker.InputProcessing.Text.Structured.Dispatching.Commands.AddTransaction.Parsing;
using MyFinanceTracker.InputProcessing.Text.Structured.Dispatching.Commands.DeleteTransaction;
using MyFinanceTracker.InputProcessing.Text.Structured.Dispatching.Commands.DeleteTransaction.Parsing;
using MyFinanceTracker.InputProcessing.Text.Structured.Dispatching.Commands.ListCategories;
using MyFinanceTracker.InputProcessing.Text.Structured.Interpretation;

namespace MyFinanceTracker.InputProcessing.Text.Structured;

public static class DependencyInjection
{
    public static IProcessorConfigured UseStructured(this ITextInputProcessingBuilder builder)
    {
        builder.Services
            .AddScoped<ITextInputProcessor, StructuredTextInputProcessor>()
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