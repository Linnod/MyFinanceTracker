using Microsoft.Extensions.Logging;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Logging;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Initialization;

using static LogEvents.Initializer;

internal partial class SqliteDatabaseInitializerHostedService
{
    [LoggerMessage(
        EventId = InitializingDatabase,
        Level = LogLevel.Information,
        Message = "Initializing SQLite database and configuring WAL mode")]
    partial void LogInitializingDatabase();

    [LoggerMessage(
        EventId = FlushingOutbox,
        Level = LogLevel.Debug,
        Message = "Flushing pending outbox messages before transactions synchronization")]
    partial void LogFlushingOutbox();

    [LoggerMessage(
        EventId = FetchingTransactions,
        Level = LogLevel.Debug,
        Message = "Fetching remote transactions for initial database synchronization")]
    partial void LogFetchingTransactions();

    [LoggerMessage(
        EventId = SyncingTransactions,
        Level = LogLevel.Debug,
        Message = "Replacing local transactions cache with {Count} transactions")]
    partial void LogSyncingTransactions(int count);

    [LoggerMessage(
        EventId = DatabaseInitialized,
        Level = LogLevel.Information,
        Message = "SQLite database initialized successfully. Synced {Count} transactions")]
    partial void LogDatabaseInitialized(int count);

    [LoggerMessage(
        EventId = StoppingInitialization,
        Level = LogLevel.Debug,
        Message = "Flushing pending outbox messages on shutdown")]
    partial void LogStoppingInitialization();

    [LoggerMessage(
        EventId = StopCompleted,
        Level = LogLevel.Debug,
        Message = "Shutdown outbox flush completed")]
    partial void LogStopCompleted();

    [LoggerMessage(
        EventId = FinalOutboxProcessingFailed,
        Level = LogLevel.Warning,
        Message = "Final outbox processing on shutdown failed")]
    partial void LogFinalOutboxProcessingFailed(Exception ex);
}
