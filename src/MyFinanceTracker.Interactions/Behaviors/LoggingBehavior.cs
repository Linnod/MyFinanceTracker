using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Interactions.Contracts;

namespace MyFinanceTracker.Interactions.Behaviors;

internal sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        if (response is InteractionResponse ir)
        {
            switch (ir)
            {
                case InteractionResponse.Success s:
                    logger.LogInformation("✅ [Interaction] {Desc} success: {Val} ({Ms}ms)", 
                        s.InteractionDescription, s.PrimaryValue, sw.ElapsedMilliseconds);
                    break;

                case InteractionResponse.UnrecognizedInteraction u:
                    logger.LogWarning("❓ [Interaction] Command not recognized: '{Input}' ({Ms}ms)", 
                        u.RawInput, sw.ElapsedMilliseconds);
                    break;

                case InteractionResponse.InvalidInput i:
                    logger.LogWarning("⚠️ [Interaction] Invalid input for {Desc}: {Details} ({Ms}ms)", 
                        i.InteractionDescription, i.Details, sw.ElapsedMilliseconds);
                    break;

                case InteractionResponse.LogicError l:
                    logger.LogWarning("❌ [Interaction] Business logic error: {Msg} ({Ms}ms)", 
                        l.Message, sw.ElapsedMilliseconds);
                    break;

                case InteractionResponse.SystemError e:
                    logger.LogError("🔌 [Interaction] Critical system failure: {Msg} ({Ms}ms)", 
                        e.Message, sw.ElapsedMilliseconds);
                    break;
            }
        }

        return response;
    }
}