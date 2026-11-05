using Microsoft.Extensions.Logging;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Logging;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Sync;

using static LogEvents.Sync;

internal partial class OutboxSyncBackgroundService
{
    [LoggerMessage(
        EventId = StartingSync,
        Level = LogLevel.Information,
        Message = "Outbox sync background service started with interval {IntervalSeconds}s")]
    partial void LogSyncStarted(int intervalSeconds);

    [LoggerMessage(
        EventId = SyncFailed,
        Level = LogLevel.Error,
        Message = "Failed to process pending outbox messages during sync")]
    partial void LogSyncFailed(Exception ex);

    [LoggerMessage(
        EventId = SyncStopped,
        Level = LogLevel.Information,
        Message = "Outbox sync background service stopped")]
    partial void LogSyncStopped();
}
