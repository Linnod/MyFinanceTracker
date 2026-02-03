using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.DeleteTransaction.Parsing;
using MyFinanceTracker.UseCases.Transaction.Delete;

namespace MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.DeleteTransaction;

internal sealed partial class DeleteTransactionCommandHandler(
    IDeleteTransactionCommandParser parser,
    IMediator mediator,
    ILogger<DeleteTransactionCommandHandler> logger)
    : BaseCommandHandler
{
    protected override string CommandName => "Cleaning a category";

    protected override TextCommandType GetHandlingCommandType() => TextCommandType.DeleteTransaction;

    public override async Task<TextCommandResponse> Handle(string payload, CancellationToken ct)
    {
        LogCommandHandlerEntry(payload);
        var response = await HandleInternal(payload, ct);

        LogCommandHandlerExit(response);
        return response;
    }

    private async Task<TextCommandResponse> HandleInternal(string payload, CancellationToken ct)
    {
        var parseResult = await parser.Parse(payload);

        return parseResult switch
        {
            DeleteTransactionCommandParseResult.Success success => await ProcessCommand(success, ct),
            DeleteTransactionCommandParseResult.Failure failure => ProcessParseFailure(failure),
            _ => throw new UnreachableException($"Unknown parse result type: {parseResult.GetType().Name}")
        };
    }

    private async Task<TextCommandResponse> ProcessCommand(
        DeleteTransactionCommandParseResult.Success success,
        CancellationToken ct)
    {
        LogParseSuccess(success);

        var raw = success.Command;
        var request = new DeleteTransactionsRequest(raw.CategoryAlias, raw.Date);
        var result = await mediator.Send(request, ct);

        return MapToResponse(result);
    }

    private TextCommandResponse MapToResponse(DeleteTransactionsResponse result)
    {
        return result switch
        {
            DeleteTransactionsResponse.Success s => MapSuccess(s),

            DeleteTransactionsResponse.ValidationError v => new TextCommandResponse.InvalidInput(
                Details: FormatValidationErrors(v.Errors),
                Suggestion: v.Errors.FirstOrDefault(e => e.Suggestion != null)?.Suggestion
            ),

            DeleteTransactionsResponse.Failure => new TextCommandResponse.SystemError("Domain service failure. Please try again later."),
            _ => throw new UnreachableException($"Unknown response type: {result.GetType().Name}")
        };
    }

    private TextCommandResponse.Success MapSuccess(DeleteTransactionsResponse.Success data)
    {
        return new TextCommandResponse.Success(
            CommandDescription: CommandName,
            PrimaryValue: $"Cleared category '{data.CategoryName}'",
            Details:
            [
                new TextCommandResponseDetail("Date", data.Date.ToString("dd.MM.yyyy"), "📅")
            ]
        );
    }

    private TextCommandResponse.InvalidInput ProcessParseFailure(
        DeleteTransactionCommandParseResult.Failure failure)
    {
        LogParseFailure(failure);

        return new TextCommandResponse.InvalidInput(failure.Reason, failure.Suggestion, failure.Examples);
    }
}