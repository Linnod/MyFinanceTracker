using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Common.Notifications;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Sync;

internal sealed partial class OutboxNotificationPublisher(
    IPublisher publisher,
    ILogger<OutboxNotificationPublisher> logger) : IOutboxNotificationPublisher
{
    public async Task Publish(OutboxSyncNotification notification, CancellationToken ct)
    {
        try
        {
            await publisher.Publish(notification, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogNotificationPublishFailed(ex);
        }
    }
}
