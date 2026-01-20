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

            return result.Match(
                onSuccess: () => InteractionResult.Success(operation),
                onFailure: error => InteractionResult.Failure(error)
            );
        }
        catch (Exception ex) when (ex is ParsingException or ValidationException)
        {
            return InteractionResult.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred while processing message: {Input}", request.RawInput);

            return InteractionResult.Failure("Unexpected system error");
        }
    }

    private static CreateTransactionRequest MapToCreateTransactionRequest(FinancialOperation financialOperation)
    {
        return new CreateTransactionRequest(
            financialOperation.Type,
            financialOperation.CategoryAlias!,
            financialOperation.Amounts,
            financialOperation.Date,
            financialOperation.Notes);
    }
}