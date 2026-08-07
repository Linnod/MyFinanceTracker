using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.CommandProcessing.Text;

public sealed partial class TextCommandReceiver(
    IServiceScopeFactory scopeFactory,
    ILogger<TextCommandReceiver> logger) : ITextCommandReceiver
{
    public async Task<TextCommandResponse> Receive(TextCommandRequest request, CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        try
        {
            LogReceiveEntry(request);

            var processor = scope.ServiceProvider.GetRequiredService<ITextCommandProcessor>();
            var response = await processor.Execute(request, ct);

            LogReceiveExit(response);
            return response;
        }
        catch (Exception ex)
        {
            LogCriticalSystemError(request.Input, ex);
            return new TextCommandResponse.SystemError("The engine encountered an unhandled error. Please try again later.");
        }
    }
}