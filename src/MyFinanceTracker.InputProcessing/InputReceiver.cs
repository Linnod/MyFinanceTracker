using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.InputProcessing;

public sealed partial class InputReceiver(
    IServiceScopeFactory scopeFactory,
    ILogger<InputReceiver> logger) : IInputReceiver
{
    public Task<ProcessingResult> Receive(Input input, CancellationToken ct)
        => input switch
        {
            Input.Text text => ReceiveTyped(text, ct),
            Input.Audio audio => ReceiveTyped(audio, ct),
            Input.Image image => ReceiveTyped(image, ct),
            _ => throw new UnreachableException($"Unhandled input type: {input.GetType().Name}")
        };

    private async Task<ProcessingResult> ReceiveTyped<TInput>(TInput input, CancellationToken ct)
        where TInput : Input
    {
        var inputTypeName = typeof(TInput).Name;
        LogReceiveEntry(input);

        using var scope = scopeFactory.CreateScope();
        try
        {
            var processor = scope.ServiceProvider.GetService<IInputProcessor<TInput>>();
            if (processor is null)
            {
                LogProcessorNotFound(inputTypeName);
                return new ProcessingResult.SystemError($"No processor registered for {inputTypeName}.");
            }

            var result = await processor.Process(input, ct);
            LogReceiveExit(result);
            return result;
        }
        catch (Exception ex)
        {
            LogCriticalSystemError(input, ex);
            return new ProcessingResult.SystemError("The engine encountered an unhandled error. Please try again later.");
        }
    }
}
