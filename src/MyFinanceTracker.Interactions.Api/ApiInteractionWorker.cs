using MediatR;
using Microsoft.Extensions.Options;

namespace MyFinanceTracker.Interactions.Api;

internal sealed class ApiInteractionWorker(
    IServiceProvider serviceProvider,
    IOptions<ApiInteractionOptions> options,
    ILogger<ApiInteractionWorker> logger) : BackgroundService
{
    private readonly ApiInteractionOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("--> [ApiInteractionWorker] Starting REST Web API on port {Port}...", _options.Port);

        var app = ApiServerBuilder.Build(_options, serviceProvider);

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