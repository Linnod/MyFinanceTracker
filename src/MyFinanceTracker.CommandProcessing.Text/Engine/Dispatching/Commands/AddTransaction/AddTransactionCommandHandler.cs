using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.AddTransaction.Parsing;
using MyFinanceTracker.UseCases.Transaction.Create;

namespace MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.AddTransaction;

internal sealed partial class AddTransactionCommandHandler(
    IAddTransactionCommandPayloadParser payloadParser,
    IMediator mediator,
    ILogger<AddTransactionCommandHandler> logger)
    : BaseCommandHandler, ICommandHandler<AddTransactionCommand>
{

    public async Task<TextCommandResponse> Handle(AddTransactionCommand command, CancellationToken ct)
    {
        LogHandlerEntry(command);
        var response = await HandleInternal(command, ct);
        LogHandlerExit(response);

        return response;
    }

    private async Task<TextCommandResponse> HandleInternal(AddTransactionCommand command, CancellationToken ct)
    {
        var commandMetadata = command.GetMetadata();
        var parseResult = await payloadParser.Parse(command.Payload);
        return parseResult switch
        {
            AddTransactionCommandParseResult.Success success => await ProcessParseSuccess(success, commandMetadata, ct),
            AddTransactionCommandParseResult.Failure failure => ProcessParseFailure(failure, commandMetadata),
            _ => throw new UnreachableException($"Unknown parse result type: {parseResult.GetType().Name}")
        };
    }

    private async Task<TextCommandResponse> ProcessParseSuccess(
        AddTransactionCommandParseResult.Success success, 
        CommandMetadataAttribute commandMetadata, 
        CancellationToken ct)
    {
        LogParseSuccess(success);

        var parsedPayload = success.Payload;
        var request = new CreateTransactionRequest(
            parsedPayload.Type,
            parsedPayload.Amounts,
            parsedPayload.CategoryAlias,
            parsedPayload.Date,
            parsedPayload.Note);

        var result = await mediator.Send(request, ct);
        return MapToResponse(result, commandMetadata);
    }

    private TextCommandResponse MapToResponse(CreateTransactionResponse result,  CommandMetadataAttribute commandMetadata)
    {
        return result switch
        {
            CreateTransactionResponse.Success s => MapSuccess(s, commandMetadata),

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

    private TextCommandResponse.Success MapSuccess(CreateTransactionResponse.Success s, CommandMetadataAttribute commandMetadata)
    {
        var totalAmount = s.Amounts.Sum();
        return new TextCommandResponse.Success(
            CommandDescription: commandMetadata.Description,
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

    private TextCommandResponse.InvalidInput ProcessParseFailure(
        AddTransactionCommandParseResult.Failure failure, 
        CommandMetadataAttribute commandMetadata)
    {
        LogParseFailure(failure);
        return new TextCommandResponse.InvalidInput(failure.Reason, commandMetadata.UsageHint, commandMetadata.Examples);
    }
}