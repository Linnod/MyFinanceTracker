using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyFinanceTracker.Common.Notifications;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Configuration;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Sync;

internal sealed partial class OutboxSyncBackgroundService(
    OutboxProcessor outboxProcessor,
    IOptions<SqlitePersistenceOptions> options,
    IOutboxNotificationPublisher notificationPublisher,
    ILogger<OutboxSyncBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogSyncStarted(options.Value.SyncIntervalSeconds);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.SyncIntervalSeconds));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await outboxProcessor.ProcessPendingMessages(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    LogSyncFailed(ex);
                    await notificationPublisher.Publish(new OutboxSyncNotification.SyncFailed(ex.Message), stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected during host shutdown
        }

        LogSyncStopped();
    }
}
