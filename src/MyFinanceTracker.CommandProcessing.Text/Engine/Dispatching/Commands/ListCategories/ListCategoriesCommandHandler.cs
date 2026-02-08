using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.UseCases.Category.List;

namespace MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.ListCategories;

internal sealed partial class ListCategoriesCommandHandler(
    IMediator mediator,
    ILogger<ListCategoriesCommandHandler> logger) : BaseCommandHandler
{
    protected override string CommandName => "Listing categories";
    protected override TextCommandType GetHandlingCommandType() => TextCommandType.ListCategories;

    public async override Task<TextCommandResponse> Handle(string payload, CancellationToken ct)
    {
        LogCommandHandlerEntry(payload);

        var response = await mediator.Send(new ListCategoriesRequest(), ct);

        var result = response switch
        {
            ListCategoriesResponse.Success success => MapSuccess(success),
            ListCategoriesResponse.Failure => new TextCommandResponse.LogicError("Domain service failure. Please try again later."),
            _ => new TextCommandResponse.SystemError("Unexpected response type from use case.")
        };

        LogCommandHandlerExit(result);
        return result;
    }

    private TextCommandResponse MapSuccess(ListCategoriesResponse.Success success)
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
            CommandDescription: CommandName,
            PrimaryValue: "Available categories and their aliases:",
            Details: details
        );
    }
}
