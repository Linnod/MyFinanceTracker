using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Interactions.Abstractions;
using MyFinanceTracker.Interactions.Interpretation;
using MyFinanceTracker.Interactions.Contracts;
using MyFinanceTracker.Interactions.Commands.AddTransaction.Parsing;
using MyFinanceTracker.UseCases.Transaction.Create;

namespace MyFinanceTracker.Interactions.Commands.AddTransaction;

internal sealed class AddTransactionCommandHandler(
    IAddTransactionCommandParser parser,
    IMediator mediator,
    ILogger<AddTransactionCommandHandler> logger)
    : IInteractionHandler
{
    private const string InteractionName = "Adding a transaction";

    public bool CanHandle(InteractionType type) => type == InteractionType.AddTransaction;

    public async Task<InteractionResponse> HandleAsync(string payload, CancellationToken ct)
    {
        var parseResult = parser.Parse(payload);
        if (parseResult is not AddTransactionCommandParseResult.Success(var raw))
        {
            return MapParseError(parseResult);
        }

        try
        {
            var request = new CreateTransactionRequest(
                raw.Type,
                raw.Amounts,
                raw.CategoryAlias,
                raw.Date,
                raw.Note);
            var result = await mediator.Send(request, ct);

            return MapToInteractionResponse(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "System failure for input: {Input}", payload);

            return new InteractionResponse.SystemError("Processing failed on our side.", ex);
        }
    }

    private static InteractionResponse MapToInteractionResponse(CreateTransactionResponse result)
    {
        return result switch
        {
            CreateTransactionResponse.Success s => MapSuccess(s),
            CreateTransactionResponse.ValidationError v => new InteractionResponse.LogicError(v.Message),
            CreateTransactionResponse.Failure f => new InteractionResponse.LogicError(f.Message),
            _ => new InteractionResponse.SystemError("Unexpected response from domain service.")
        };
    }

    private static InteractionResponse.Success MapSuccess(CreateTransactionResponse.Success s)
    {
        var totalAmount = s.Amounts.Sum();

        return new InteractionResponse.Success(
            InteractionDescription: InteractionName,
            PrimaryValue: $"{totalAmount} added to category '{s.CategoryName}'",
            Details: BuildDetails(s)
        );
    }

    private static List<ResponseDetail> BuildDetails(CreateTransactionResponse.Success s)
    {
        var details = new List<ResponseDetail>
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

    private static InteractionResponse.InvalidInput MapParseError(AddTransactionCommandParseResult result)
    {
        var message = result switch
        {
            AddTransactionCommandParseResult.EmptyInput => "The input string is empty.",
            AddTransactionCommandParseResult.InvalidFormat => "Format error. Use: <type> <amounts> <category?> <date?> <note?>",
            AddTransactionCommandParseResult.InvalidAmount(var v) => $"'{v}' is not a valid amount.",
            AddTransactionCommandParseResult.UnparseableDate(var v) => $"'{v}' is not a valid date format.",
            _ => "Unknown parsing error."
        };

        return new InteractionResponse.InvalidInput(InteractionName, message);
    }
}