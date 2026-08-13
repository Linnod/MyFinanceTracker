using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.InputProcessing.Text.Structured.Dispatching.Commands.AddTransaction.Parsing;
using MyFinanceTracker.UseCases.Transaction.Create;

namespace MyFinanceTracker.InputProcessing.Text.Structured.Dispatching.Commands.AddTransaction;

internal sealed partial class AddTransactionCommandHandler(
    IAddTransactionCommandPayloadParser payloadParser,
    IMediator mediator,
    ILogger<AddTransactionCommandHandler> logger)
    : ICommandHandler<AddTransactionCommand>
{
    public async Task<CommandExecutionResult> Handle(AddTransactionCommand command, CancellationToken ct)
    {
        LogHandlerEntry(command);

        var parseResult = await payloadParser.Parse(command.Payload);
        CommandExecutionResult action = parseResult switch
        {
            AddTransactionCommandParseResult.Success success => 
                await ProcessParseSuccess(success, ct),

            AddTransactionCommandParseResult.Failure failure => 
                new CommandExecutionResult.InvalidSyntax(
                    ErrorCode: failure.ErrorCode,
                    Examples: command.GetMetadata().Examples
                ),

            _ => throw new UnreachableException($"Unknown parse result type: {parseResult.GetType().Name}")
        };

        LogHandlerExit(action);
        return action;
    }

    private async Task<CommandExecutionResult> ProcessParseSuccess(
        AddTransactionCommandParseResult.Success success, 
        CancellationToken ct)
    {
        var payload = success.Payload;
        var items = payload.Amounts.Select(amount => new CreateTransactionItem(
            TransactionType: payload.Type,
            Amount: amount,
            CategoryAlias: payload.CategoryAlias,
            Date: payload.Date,
            Note: payload.Note
        )).ToList();

        var response = await mediator.Send(new CreateTransactionsRequest(items), ct);

        return response switch
        {
            CreateTransactionsResponse.Success s => 
                new CommandExecutionResult.Transaction.Added(s.Transactions),

            CreateTransactionsResponse.ValidationError v => 
                new CommandExecutionResult.InvalidInput(
                    Errors: v.Errors
                ),

            CreateTransactionsResponse.Failure => 
                new CommandExecutionResult.Failure(),

            _ => throw new UnreachableException($"Unknown response type: {response.GetType().Name}")
        };
    }
}