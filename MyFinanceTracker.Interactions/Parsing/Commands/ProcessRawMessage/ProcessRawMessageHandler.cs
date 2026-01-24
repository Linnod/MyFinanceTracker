using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Interactions.Contracts;
using MyFinanceTracker.Interactions.Parsing.Parser.Exceptions;
using MyFinanceTracker.Interactions.Parsing.Validation.Exceptions;
using MyFinanceTracker.UseCases.Transaction.Create;
using System.Diagnostics;

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

            return result switch
            {
                CreateTransactionResult.Success => new InteractionResult.Success(operation),
                CreateTransactionResult.Failure f => new InteractionResult.LogicError(f.Message),
                _ => throw new UnreachableException($"Unhandled result type: {result.GetType()}")
            };
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