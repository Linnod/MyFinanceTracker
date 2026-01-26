using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Interactions.Contracts;

namespace MyFinanceTracker.Interactions.Behaviors;

internal sealed class InteractionLoggingBehavior(
    ILogger<InteractionLoggingBehavior> logger)
    : IPipelineBehavior<InteractionRequest, InteractionResponse>
{
    public async Task<InteractionResponse> Handle(
        InteractionRequest request, 
        RequestHandlerDelegate<InteractionResponse> next, 
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        var elapsedMs = sw.ElapsedMilliseconds;
        var input = request.Input;

        switch (response)
        {
            case InteractionResponse.Success s:
                logger.LogInformation("✅ [Interaction] {Desc} success: {Val} | Input: '{Input}' ({Ms}ms)", 
                    s.InteractionDescription, s.PrimaryValue, input, elapsedMs);
                break;

            case InteractionResponse.UnrecognizedInteraction:
                logger.LogWarning("❓ [Interaction] Command not recognized | Input: '{Input}' ({Ms}ms)", 
                    input, elapsedMs);
                break;

            case InteractionResponse.InvalidInput i:
                logger.LogWarning("⚠️ [Interaction] Invalid input for {Desc}: {Details} | Input: '{Input}' ({Ms}ms)", 
                    i.InteractionDescription, i.Details, input, elapsedMs);
                break;

            case InteractionResponse.LogicError l:
                logger.LogWarning("❌ [Interaction] Business logic error: {Msg} | Input: '{Input}' ({Ms}ms)", 
                    l.Message, input, elapsedMs);
                break;

            case InteractionResponse.SystemError e:
                logger.LogError(e.Exception, "🔌 [Interaction] Critical system failure: {Msg} | Input: '{Input}' ({Ms}ms)", 
                    e.Message, input, elapsedMs);
                break;
        }

        return response;
    }
}