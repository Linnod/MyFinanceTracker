using MyFinanceTracker.Common.Notifications;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Sync;

internal interface IOutboxNotificationPublisher
{
    Task Publish(OutboxSyncNotification notification, CancellationToken ct);
}
