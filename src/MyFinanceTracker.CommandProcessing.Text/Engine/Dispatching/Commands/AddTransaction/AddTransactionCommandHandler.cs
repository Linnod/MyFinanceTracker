using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.AddTransaction.Parsing;
using MyFinanceTracker.UseCases.Transaction.Create;

namespace MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.AddTransaction;

internal sealed partial class AddTransactionCommandHandler(
    IAddTransactionCommandParser parser,
    IMediator mediator,
    ILogger<AddTransactionCommandHandler> logger)
    : BaseCommandHandler
{
    protected override string CommandName => "Adding a transaction";

    protected override TextCommandType GetHandlingCommandType() => TextCommandType.AddTransaction;

    public override async Task<TextCommandResponse> Handle(string payload, CancellationToken ct)
    {
        LogHandlerEntry(payload);
        var response = await HandleInternal(payload, ct);
        LogHandlerExit(response);

        return response;
    }

    private async Task<TextCommandResponse> HandleInternal(string payload, CancellationToken ct)
    {
        var parseResult = await parser.Parse(payload);

        return parseResult switch
        {
            AddTransactionCommandParseResult.Success success => await ProcessCommand(success, ct),
            AddTransactionCommandParseResult.Failure failure => ProcessParseFailure(failure),
            _ => throw new UnreachableException($"Unknown parse result type: {parseResult.GetType().Name}")
        };
    }

    private async Task<TextCommandResponse> ProcessCommand(AddTransactionCommandParseResult.Success success, CancellationToken ct)
    {
        LogParseSuccess(success);

        var command = success.Command;
        var request = new CreateTransactionRequest(
            command.Type,
            command.Amounts,
            command.CategoryAlias,
            command.Date,
            command.Note);

        var result = await mediator.Send(request, ct);
        return MapToResponse(result);
    }

    private TextCommandResponse MapToResponse(CreateTransactionResponse result)
    {
        return result switch
        {
            CreateTransactionResponse.Success s => MapSuccess(s),

            CreateTransactionResponse.ValidationError v =>
                new TextCommandResponse.InvalidInput(
                    Details: FormatValidationErrors(v.Errors),
                    Suggestion: v.Errors.FirstOrDefault(e => e.Suggestion != null)?.Suggestion
                ),

            CreateTransactionResponse.Failure => new TextCommandResponse.SystemError(
                "Domain service failure. Please try again later."),

            _ => throw new UnreachableException($"Unknown response type: {result.GetType().Name}")
        };
    }

    private TextCommandResponse.Success MapSuccess(CreateTransactionResponse.Success s)
    {
        var totalAmount = s.Amounts.Sum();
        return new TextCommandResponse.Success(
            CommandDescription: CommandName,
            PrimaryValue: $"{totalAmount} added to category '{s.CategoryName}'",
            Details: BuildDetails(s)
        );
    }

    private static List<TextCommandResponseDetail> BuildDetails(CreateTransactionResponse.Success s)
    {
        var details = new List<TextCommandResponseDetail>
        {
            new("Date", s.Date.ToString("dd.MM.yyyy"), "📅")
        };

        if (s.Amounts.Count > 1)
        {
            details.Add(new("Breakdown", string.Join(" + ", s.Amounts), "🔢"));
        }

        if (!string.IsNullOrWhiteSpace(s.Note))
        {
            details.Add(new("Note", s.Note, "📝"));
        }

        return details;
    }

    private TextCommandResponse.InvalidInput ProcessParseFailure(AddTransactionCommandParseResult.Failure failure)
    {
        LogParseFailure(failure);
        return new TextCommandResponse.InvalidInput(CommandName, failure.Message);
    }
}