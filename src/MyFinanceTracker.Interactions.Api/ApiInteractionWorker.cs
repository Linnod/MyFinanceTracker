using Microsoft.Extensions.Options;
using MyFinanceTracker.CommandProcessing.Text;

namespace MyFinanceTracker.Interactions.Api;

internal sealed class ApiInteractionWorker(
    ITextCommandReceiver textCommandReceiver,
    IOptions<ApiInteractionOptions> options,
    ILogger<ApiInteractionWorker> logger) : BackgroundService
{
    private readonly ApiInteractionOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("--> [ApiInteractionWorker] Starting Web API on port {Port}...", _options.Port); //TODO: use LogEventRanges

        var app = ApiServerBuilder.Build(_options, textCommandReceiver);

        try
        {
            await app.RunAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("--> [ApiInteractionWorker] Web API stopped.");
        }
    }
}