using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.DeleteTransaction.Parsing;
using MyFinanceTracker.UseCases.Transaction.Delete;

namespace MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.DeleteTransaction;

internal sealed partial class DeleteTransactionCommandHandler(
    IDeleteTransactionCommandPayloadParser parser,
    IMediator mediator,
    ILogger<DeleteTransactionCommandHandler> logger)
    : BaseCommandHandler, ICommandHandler<DeleteTransactionCommand>
{

    public async Task<TextCommandResponse> Handle(DeleteTransactionCommand command, CancellationToken ct)
    {
        LogCommandHandlerEntry(command);
        var response = await HandleInternal(command, ct);

        LogCommandHandlerExit(response);
        return response;
    }

    private async Task<TextCommandResponse> HandleInternal(DeleteTransactionCommand command, CancellationToken ct)
    {
        var commandMetadata = command.GetMetadata();
        var parseResult = await parser.Parse(command.Payload);
        return parseResult switch
        {
            DeleteTransactionCommandParseResult.Success success => await ProcessParseSuccess(success, commandMetadata, ct),
            DeleteTransactionCommandParseResult.Failure failure => ProcessParseFailure(failure, commandMetadata),
            _ => throw new UnreachableException($"Unknown parse result type: {parseResult.GetType().Name}")
        };
    }

    private async Task<TextCommandResponse> ProcessParseSuccess(
        DeleteTransactionCommandParseResult.Success success,
        CommandMetadataAttribute commandMetadata,
        CancellationToken ct)
    {
        LogParseSuccess(success);

        var parsedPayload = success.Payload;
        var request = new DeleteTransactionsRequest(parsedPayload.CategoryAlias, parsedPayload.Date);
        var result = await mediator.Send(request, ct);

        return MapToResponse(result, commandMetadata);
    }

    private TextCommandResponse MapToResponse(DeleteTransactionsResponse result,  CommandMetadataAttribute commandMetadata)
    {
        return result switch
        {
            DeleteTransactionsResponse.Success s => MapSuccess(s, commandMetadata),

            DeleteTransactionsResponse.ValidationError v => new TextCommandResponse.InvalidInput(
                Details: FormatValidationErrors(v.Errors),
                Suggestion: v.Errors.FirstOrDefault(e => e.Suggestion != null)?.Suggestion
            ),

            DeleteTransactionsResponse.Failure => new TextCommandResponse.SystemError("Domain service failure. Please try again later."),
            _ => throw new UnreachableException($"Unknown response type: {result.GetType().Name}")
        };
    }

    private TextCommandResponse.Success MapSuccess(DeleteTransactionsResponse.Success data, CommandMetadataAttribute commandMetadata)
    {
        return new TextCommandResponse.Success(
            CommandDescription: commandMetadata.Description,
            PrimaryValue: $"Cleared category '{data.CategoryName}'",
            Details:
            [
                new TextCommandResponseDetail("Date", data.Date.ToString("dd.MM.yyyy"), "📅")
            ]
        );
    }

    private TextCommandResponse.InvalidInput ProcessParseFailure(
        DeleteTransactionCommandParseResult.Failure failure,
        CommandMetadataAttribute commandMetadata)
    {
        LogParseFailure(failure);

        return new TextCommandResponse.InvalidInput(failure.Reason, commandMetadata.UsageHint, commandMetadata.Examples);
    }
}