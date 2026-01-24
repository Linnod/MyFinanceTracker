using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Common.Contracts;
using MyFinanceTracker.Interactions.Contracts;

namespace MyFinanceTracker.Interactions.Behaviors;

internal sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        var payload = request is ILoggableRequest loggable ? loggable.GetLogPayload() : "N/A";

        logger.LogInformation("🚀 Processing {Name} | Data: {Payload}", requestName, payload);

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await next();
            sw.Stop();

            if (response is InteractionResult result)
            {
                LogResult(requestName, result, sw.ElapsedMilliseconds);
            }
            else
            {
                logger.LogInformation("✅ Completed {Name} in {Elapsed}ms", requestName, sw.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "💥 Failed {Name} after {Elapsed}ms", requestName, sw.ElapsedMilliseconds);

            throw;
        }
    }

    private void LogResult(string reqName, InteractionResult result, long ms)
    {
        result.Match(
            onSuccess: s => logger.LogInformation(
                "✅ {Req} SUCCESS | {Ms}ms | Op: {Type} ({Amount}€)",
                reqName, ms, s.Operation.Type, s.Operation.Amounts.Sum()),

            onParseError: e => logger.LogWarning(
                "❓ {Req} PARSE ERROR | {Ms}ms | Input: '{Input}' | Details: {Details}",
                reqName, ms, e.RawInput, e.Details),

            onLogicError: e => logger.LogWarning(
                "⚠️ {Req} LOGIC ERROR | {Ms}ms | {Msg}",
                reqName, ms, e.Message),

            onSystemError: e => logger.LogError(
                "🔌 {Req} SYSTEM ERROR | {Ms}ms | {Msg}",
                reqName, ms, e.Message)
        );
    }
}