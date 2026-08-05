using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MyFinanceTracker.CommandProcessing.Text;

namespace MyFinanceTracker.Interactions.Api;

internal static class ApiInteractionEndpoints
{
    public static async Task<IResult> GetCategories(
        [FromServices] ITextCommandReceiver receiver,
        CancellationToken ct)
    {
        var response = await receiver.Receive(new TextCommandRequest("c list"), ct);

        return response switch
        {
            TextCommandResponse.Success success => Results.Ok(new
            {
                Status = "Success",
                Message = success.PrimaryValue,
                Categories = success.Details.Select(d => new
                {
                    d.Name,
                    Aliases = d.Value,
                    Type = d.Icon == "💰" ? "Income" : "Expense" //TODO: fix relying on icon
                })
            }),
            _ => Results.Problem("Failed to retrieve categories")
        };
    }

    public static async Task<IResult> ExecuteCommand(
        [FromBody] ProcessCommandRequest request,
        [FromServices] ITextCommandReceiver receiver,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return Results.BadRequest(new
            {
                Status = "InvalidInput",
                Error = "Command text cannot be empty."
            });
        }

        var response = await receiver.Receive(new TextCommandRequest(request.Text), ct);

        return MapResponseToResult(response);
    }

    private static IResult MapResponseToResult(TextCommandResponse response) => response switch
    {
        TextCommandResponse.Success success => Results.Ok(new
        {
            Status = "Success",
            success.PrimaryValue,
            success.Details
        }),

        TextCommandResponse.InvalidInput invalid => Results.BadRequest(new
        {
            Status = "InvalidInput",
            Error = invalid.Details,
            invalid.Suggestion,
            invalid.Examples
        }),

        TextCommandResponse.LogicError logicError => Results.UnprocessableEntity(new
        {
            Status = "LogicError",
            logicError.Message
        }),

        TextCommandResponse.SystemError systemError => Results.Problem(
            title: "System Error",
            detail: systemError.Message),

        _ => throw new UnreachableException($"Unknown response type: {response.GetType()}")
    };
}