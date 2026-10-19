using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.InputProcessing.Inputs;

namespace MyFinanceTracker.InputProcessing;

public sealed partial class InputReceiver(
    IServiceScopeFactory scopeFactory,
    ILogger<InputReceiver> logger) : IInputReceiver
{
    public Task<ProcessingResult> Receive(IInput input, CancellationToken ct)
        => input switch
        {
            TextInput text => ReceiveTyped(text, ct),
            AudioInput audio => ReceiveTyped(audio, ct),
            _ => Task.FromResult<ProcessingResult>(
                new ProcessingResult.SystemError($"Unsupported input type: {input.GetType().Name}."))
        };

    private async Task<ProcessingResult> ReceiveTyped<TInput>(TInput input, CancellationToken ct)
        where TInput : IInput
    {
        var inputTypeName = typeof(TInput).Name;
        LogReceiveEntry(input);

        using var scope = scopeFactory.CreateScope();
        try
        {
            var processor = scope.ServiceProvider.GetService<IInputProcessor<TInput>>();
            if (processor is null)
            {
                LogProcessorNotFound(typeof(TInput).Name);
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
