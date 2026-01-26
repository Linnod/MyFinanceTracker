namespace MyFinanceTracker.Interactions.Dispatcher;

using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Interactions.Abstractions;
using MyFinanceTracker.Interactions.Contracts;
using MyFinanceTracker.Interactions.Interpretation;

internal sealed class InteractionDispatcher(
    IInteractionInterpreter interpreter,
    IEnumerable<IInteractionHandler> handlers,
    ILogger<InteractionDispatcher> logger)
    : IRequestHandler<InteractionRequest, InteractionResponse>
{
    public async Task<InteractionResponse> Handle(
        InteractionRequest request, 
        CancellationToken ct)
    {
        var result = interpreter.Interpret(request.Input);

        return result switch
        {
            InterpretationResult.Identified identified => await ProcessIdentified(identified, ct),
            
            InterpretationResult.Unrecognized => new InteractionResponse.UnrecognizedInteraction(request.Input),
            
            _ => throw new InvalidOperationException($"Unexpected interpretation result type: {result.GetType().Name}")
        };
    }

    private async Task<InteractionResponse> ProcessIdentified(
        InterpretationResult.Identified identified, 
        CancellationToken ct)
    {
        var handler = handlers.FirstOrDefault(h => h.CanHandle(identified.Type));

        if (handler == null)
        {
            logger.LogCritical("No handler registered for interaction type: {Type}. Check DI container.", identified.Type);

            return new InteractionResponse.SystemError(
                "The requested action is recognized, but the handling mechanism is not yet implemented or is currently disabled.");
        }

        try
        {
            return await handler.HandleAsync(identified.Payload, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Execution failed for interaction type: {Type}", identified.Type);
            
            return new InteractionResponse.SystemError(
                "An error occurred while executing the command. Please try again later.", ex);
        }
    }
}