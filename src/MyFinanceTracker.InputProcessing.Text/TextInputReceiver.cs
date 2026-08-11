using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.InputProcessing.Text;

public sealed partial class TextInputReceiver(
    IServiceScopeFactory scopeFactory,
    ILogger<TextInputReceiver> logger) : ITextInputReceiver
{
    public async Task<ProcessingResult> Receive(TextInput input, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        try
        {
            LogReceiveEntry(input);

            var processor = scope.ServiceProvider.GetRequiredService<ITextInputProcessor>();
            var response = await processor.Process(input, ct);

            LogReceiveExit(response);
            return response;
        }
        catch (Exception ex)
        {
            LogCriticalSystemError(input, ex);
            return new ProcessingResult.SystemError("The engine encountered an unhandled error. Please try again later.");
        }
    }
}