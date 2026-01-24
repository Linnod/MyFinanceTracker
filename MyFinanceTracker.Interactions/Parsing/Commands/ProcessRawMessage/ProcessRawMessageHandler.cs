using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Interactions.Contracts;
using MyFinanceTracker.Interactions.Parsing.Parser.Exceptions;
using MyFinanceTracker.Interactions.Parsing.Validation.Exceptions;
using MyFinanceTracker.UseCases.Transaction.Create;

namespace MyFinanceTracker.Interactions.Parsing.Commands.ProcessRawMessage;

internal sealed class ProcessRawMessageHandler(
    ParsingService parsingService,
    IMediator mediator,
    ILogger<ProcessRawMessageHandler> logger)
    : IRequestHandler<ProcessRawMessageCommand, InteractionResult>
{
    public async Task<InteractionResult> Handle(ProcessRawMessageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var operation = parsingService.Process(request.RawInput);
            var result = await mediator.Send(MapToCreateTransactionRequest(operation), cancellationToken);

            return result.Match<InteractionResult>(
                onSuccess: () => new InteractionResult.Success(operation),
                onFailure: error => new InteractionResult.LogicError(error)
            );
        }
        catch (ParsingException ex)
        {
            return new InteractionResult.ParseError(request.RawInput, ex.Message);
        }
        catch (ValidationException ex)
        {
            return new InteractionResult.LogicError($"Validation failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for input: {Input}", request.RawInput);
            return new InteractionResult.SystemError("Something went wrong on our side.", ex);
        }
    }

    private static CreateTransactionRequest MapToCreateTransactionRequest(FinancialOperation op)
    {
        return new CreateTransactionRequest(
            op.Type,
            op.CategoryAlias,
            op.Amounts,
            op.Date,
            op.Notes);
    }
}