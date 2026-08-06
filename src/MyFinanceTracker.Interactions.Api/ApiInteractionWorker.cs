using Microsoft.Extensions.Options;

namespace MyFinanceTracker.Interactions.Api;

internal sealed partial class ApiInteractionWorker(
    IServiceProvider serviceProvider,
    IOptions<ApiInteractionOptions> options,
    ILogger<ApiInteractionWorker> logger) : BackgroundService
{
    private readonly ApiInteractionOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarted(_options.Port);

        var app = ApiServerBuilder.Build(_options, serviceProvider);

        try
        {
            await app.RunAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            LogStopped();
        }
    }
}