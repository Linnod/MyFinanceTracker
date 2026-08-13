using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.UseCases.Category.List;

namespace MyFinanceTracker.InputProcessing.Text.Structured.Dispatching.Commands.ListCategories;

internal sealed partial class ListCategoriesCommandHandler(
    IMediator mediator,
    ILogger<ListCategoriesCommandHandler> logger)
     : ICommandHandler<ListCategoriesCommand>
{
    public async Task<CommandExecutionResult> Handle(ListCategoriesCommand command, CancellationToken ct)
    {
        LogHandlerEntry();

        var response = await mediator.Send(new ListCategoriesRequest(), ct);

        CommandExecutionResult action = response switch
        {
            ListCategoriesResponse.Success success => MapSuccess(success),
            ListCategoriesResponse.Failure => new CommandExecutionResult.Failure(),
            _ => throw new UnreachableException($"Unknown response type: {response.GetType().Name}")
        };

        LogHandlerExit(action);
        return action;
    }

    private static CommandExecutionResult.Category.Listed MapSuccess(ListCategoriesResponse.Success success)
    {
        var categories = success.Categories
            .OrderByDescending(c => c.IsIncome)
            .ThenBy(c => c.Name)
            .ToList();

        return new CommandExecutionResult.Category.Listed(categories);
    }
}