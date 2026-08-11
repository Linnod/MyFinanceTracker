using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.InputProcessing.Text.Regex.Dispatching.Commands.DeleteTransaction.Parsing;
using MyFinanceTracker.UseCases.Transaction.Delete;

namespace MyFinanceTracker.InputProcessing.Text.Regex.Dispatching.Commands.DeleteTransaction;

internal sealed partial class DeleteTransactionCommandHandler(
    IDeleteTransactionCommandPayloadParser parser,
    IMediator mediator,
    ILogger<DeleteTransactionCommandHandler> logger)
    : ICommandHandler<DeleteTransactionCommand>
{
    public async Task<ActionResult> Handle(DeleteTransactionCommand command, CancellationToken ct)
    {
        LogHandlerEntry(command);

        var parseResult = await parser.Parse(command.Payload);

        var action = parseResult switch
        {
            DeleteTransactionCommandParseResult.Success success => 
                await ProcessParseSuccess(success, ct),

            DeleteTransactionCommandParseResult.Failure failure => 
                new ActionResult.InvalidSyntax(
                    ErrorCode: failure.ErrorCode,
                    Examples: command.GetMetadata().Examples
                ),

            _ => throw new UnreachableException($"Unknown parse result type: {parseResult.GetType().Name}")
        };

        LogHandlerExit(action);
        return action;
    }

    private async Task<ActionResult> ProcessParseSuccess(
        DeleteTransactionCommandParseResult.Success success,
        CancellationToken ct)
    {
        var parsedPayload = success.Payload;
        var response = await mediator.Send(new DeleteTransactionsRequest(parsedPayload.CategoryAlias, parsedPayload.Date), ct);

        return response switch
        {
            DeleteTransactionsResponse.Success s => 
                new ActionResult.Transaction.Deleted(s.CategoryName, s.Date),

            DeleteTransactionsResponse.ValidationError v => 
                new ActionResult.InvalidInput(v.Errors),

            DeleteTransactionsResponse.Failure => 
                new ActionResult.Failure(),

            _ => throw new UnreachableException($"Unknown response type: {response.GetType().Name}")
        };
    }
}