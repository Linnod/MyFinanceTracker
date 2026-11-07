using Microsoft.Extensions.Logging;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Logging;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Sync;

using static LogEvents.Sync;

internal partial class OutboxNotificationPublisher
{
    [LoggerMessage(
        EventId = NotificationPublishFailed,
        Level = LogLevel.Error,
        Message = "Failed to publish outbox sync notification")]
    partial void LogNotificationPublishFailed(Exception ex);
}
