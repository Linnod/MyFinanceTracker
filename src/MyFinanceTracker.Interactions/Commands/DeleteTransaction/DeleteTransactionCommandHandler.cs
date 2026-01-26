using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Interactions.Abstractions;
using MyFinanceTracker.Interactions.Interpretation;
using MyFinanceTracker.Interactions.Contracts;
using MyFinanceTracker.Interactions.Commands.DeleteTransaction.Parsing;
using MyFinanceTracker.UseCases.Transaction.Delete;

namespace MyFinanceTracker.Interactions.Commands.DeleteTransaction;

internal sealed class DeleteTransactionCommandHandler(
    IDeleteTransactionCommandParser parser,
    IMediator mediator,
    ILogger<DeleteTransactionCommandHandler> logger)
    : IInteractionHandler
{
    private const string InteractionName = "Cleaning a category";

    public bool CanHandle(InteractionType type) => type == InteractionType.DeleteTransaction;

    public async Task<InteractionResponse> HandleAsync(string payload, CancellationToken ct)
    {
        var parseResult = parser.Parse(payload);
        if (parseResult is not DeleteTransactionCommandParseResult.Success(var raw))
        {
            return MapParseError(parseResult);
        }

        try
        {
            var request = new DeleteTransactionsRequest(raw.CategoryAlias, raw.Date);
            var result = await mediator.Send(request, ct);

            return result switch
            {
                DeleteTransactionsResponse.Success s => MapSuccess(s),
                DeleteTransactionsResponse.ValidationError v => Invalid(v.Message),
                DeleteTransactionsResponse.Failure f => new InteractionResponse.LogicError(f.Message),
                _ => new InteractionResponse.SystemError("Unexpected response from domain service.")
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "System failure while deleting for input: {Input}", payload);
            
            return new InteractionResponse.SystemError("Processing failed on our side.", ex);
        }
    }

    private static InteractionResponse.Success MapSuccess(DeleteTransactionsResponse.Success data)
    {
        return new InteractionResponse.Success(
            InteractionDescription: "Category cleared",
            PrimaryValue: data.CategoryName,
            Details:
            [
                new ResponseDetail("Date", data.Date.ToString("dd.MM.yyyy"), "📅"),
                new ResponseDetail("Status", "All entries removed", "🧹")
            ]
        );
    }

    private static InteractionResponse.InvalidInput MapParseError(DeleteTransactionCommandParseResult result)
    {
        var message = result switch
        {
            DeleteTransactionCommandParseResult.EmptyInput => "The input string is empty.",
            DeleteTransactionCommandParseResult.InvalidFormat => "Format error. Use: rem <category> <date?>",
            DeleteTransactionCommandParseResult.UnparseableDate(var v) => $"'{v}' is not a valid date format.",
            _ => "Unknown parsing error."
        };

        return Invalid(message);
    }

    private static InteractionResponse.InvalidInput Invalid(string message) => new(InteractionName, message);
}