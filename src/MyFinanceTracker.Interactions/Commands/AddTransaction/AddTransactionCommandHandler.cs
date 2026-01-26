using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Interactions.Abstractions;
using MyFinanceTracker.Interactions.Interpretation;
using MyFinanceTracker.Interactions.Contracts;
using MyFinanceTracker.Interactions.Commands.AddTransaction.Parsing;
using MyFinanceTracker.Interactions.Commands.AddTransaction.Validation;
using MyFinanceTracker.UseCases.Transaction.Create;

namespace MyFinanceTracker.Interactions.Commands.AddTransaction;

internal sealed class AddTransactionCommandHandler(
    IAddTransactionCommandParser parser,
    IAddTransactionCommandValidator validator,
    IMediator mediator,
    ILogger<AddTransactionCommandHandler> logger)
    : IInteractionHandler
{
    private const string InteractionName = "Adding a transaction";

    public bool CanHandle(InteractionType type) => type == InteractionType.AddTransaction;

    public async Task<InteractionResponse> HandleAsync(string payload, CancellationToken ct)
    {
        var parseResult = parser.Parse(payload);
        if (parseResult is not AddTransactionCommandParseResult.Success(var rawData))
        {
            return MapToInvalidInput(parseResult);
        }

        var validationResult = validator.Validate(rawData);
        if (validationResult is not AddTransactionCommandValidationResult.Success(var validatedData))
        {
            return MapToLogicError(validationResult);
        }

        try
        {
            var createRequest = new CreateTransactionRequest(
                validatedData.Type,
                validatedData.CategoryAlias,
                validatedData.Amounts,
                validatedData.Date,
                validatedData.Note);
            var result = await mediator.Send(createRequest, ct);

            return result switch
            {
                CreateTransactionResult.Success => BuildSuccessResponse(validatedData),
                CreateTransactionResult.Failure f => new InteractionResponse.LogicError(f.Message),
                _ => new InteractionResponse.SystemError("Unexpected response from domain service.")
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "System failure for input: {Input}", payload);

            return new InteractionResponse.SystemError("Processing failed on our side.", ex);
        }
    }

    private static InteractionResponse.Success BuildSuccessResponse(ValidatedAddTransactionCommand data)
    {
        var total = data.Amounts.Sum();
        var details = new List<ResponseDetail>
        {
            new("Category", data.CategoryAlias, "📁"),
            new("Date", data.Date.ToString("dd.MM.yyyy"), "📅")
        };

        if (!string.IsNullOrWhiteSpace(data.Note))
        {
            details.Add(new ResponseDetail("Note", data.Note, "📝"));
        }

        if (data.Amounts.Length > 1)
        {
            details.Add(new ResponseDetail("Breakdown", string.Join(" + ", data.Amounts), "🔢"));
        }

        return new InteractionResponse.Success(
            InteractionDescription: $"Added {data.Type.ToString().ToLower()}",
            PrimaryValue: total.ToString("N2"),
            Details: details
        );
    }

    private static InteractionResponse.InvalidInput MapToInvalidInput(AddTransactionCommandParseResult result)
    {
        var errorDetail = result switch
        {
            AddTransactionCommandParseResult.EmptyInput => "The input string is empty.",
            AddTransactionCommandParseResult.InvalidFormat => "Format error. Use: add <type> <category?> <amounts> <date?>",
            AddTransactionCommandParseResult.InvalidAmount(var v) => $"'{v}' is not a valid amount.",
            AddTransactionCommandParseResult.UnparseableDate(var v) => $"'{v}' is not a valid date format.",
            AddTransactionCommandParseResult.DateBelowMinLimit(var d) => $"Date {d:dd.MM.yyyy} is too far in the past.",
            AddTransactionCommandParseResult.DateAboveMaxLimit(var d) => $"Date {d:dd.MM.yyyy} is in the future.",
            _ => "Unknown parsing error."
        };

        return new InteractionResponse.InvalidInput(InteractionName, errorDetail);
    }

    private static InteractionResponse.LogicError MapToLogicError(AddTransactionCommandValidationResult result)
    {
        var message = result switch
        {
            AddTransactionCommandValidationResult.MissingAmounts => "At least one amount is required.",
            AddTransactionCommandValidationResult.CategoryRequired(var t) => $"Category is required for {t.ToString().ToLower()} transactions.",
            _ => "Validation failed."
        };

        return new InteractionResponse.LogicError(message);
    }
}