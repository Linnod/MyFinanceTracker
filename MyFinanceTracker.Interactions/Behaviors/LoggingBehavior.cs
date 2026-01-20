using MediatR;
using MyFinanceTracker.Interactions.Contracts;
using System.Diagnostics;

namespace MyFinanceTracker.Interactions.Behaviors;

internal class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[LOG] Executing: {typeof(TRequest).Name}");
        
        if (request is ProcessRawMessageCommand rawCommand)
        {
            Console.WriteLine($"[LOG] Input string: '{rawCommand.RawInput}'");
        }

        var stopwatch = Stopwatch.StartNew();
        
        var response = await next();

        stopwatch.Stop();
        
        Console.WriteLine($"[LOG] Done in {stopwatch.ElapsedMilliseconds}ms");

        return response;
    }
}