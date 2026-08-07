using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.UseCases.Category.List;

namespace MyFinanceTracker.CommandProcessing.Text.Regex.Dispatching.Commands.ListCategories;

internal sealed partial class ListCategoriesCommandHandler(
    IMediator mediator,
    ILogger<ListCategoriesCommandHandler> logger)
     : BaseCommandHandler, ICommandHandler<ListCategoriesCommand>
{

    public async Task<TextCommandResponse> Handle(ListCategoriesCommand command, CancellationToken ct)
    {
        LogCommandHandlerEntry();

        var response = await mediator.Send(new ListCategoriesRequest(), ct);

        var result = response switch
        {
            ListCategoriesResponse.Success success => MapSuccess(success, command.GetMetadata()),
            ListCategoriesResponse.Failure => new TextCommandResponse.LogicError("Domain service failure. Please try again later."),
            _ => new TextCommandResponse.SystemError("Unexpected response type from use case.")
        };

        LogCommandHandlerExit(result);
        return result;
    }

    private TextCommandResponse MapSuccess(ListCategoriesResponse.Success success, CommandMetadataAttribute commandMetadata)
    {
        var details = success.Categories
            .OrderByDescending(c => c.IsIncome)
            .ThenBy(c => c.Name)
            .Select(c => new TextCommandResponseDetail(
                Name: $"{c.Name}({c.Id})",
                Value: string.Join(", ", c.Aliases),
                Icon: c.IsIncome ? "💰" : "💸"
            ))
            .ToList();

        return new TextCommandResponse.Success(
            CommandDescription: commandMetadata.Description,
            PrimaryValue: "Available categories and their aliases:",
            Details: details
        );
    }
}