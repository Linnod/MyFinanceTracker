using Microsoft.Extensions.Logging;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Logging;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Repositories;

using static LogEvents.Repository;

internal partial class SqliteTransactionRepository
{
    [LoggerMessage(
        EventId = AddingTransactions,
        Level = LogLevel.Debug,
        Message = "Adding {Count} transactions to SQLite")]
    partial void LogAddingTransactions(int count);

    [LoggerMessage(
        EventId = DeletingTransactions,
        Level = LogLevel.Debug,
        Message = "Deleting {Count} transactions from SQLite")]
    partial void LogDeletingTransactions(int count);

    [LoggerMessage(
        EventId = GettingTransactions,
        Level = LogLevel.Debug,
        Message = "Getting transactions from SQLite with filter {Filter}")]
    partial void LogGettingTransactions(TransactionFilter filter);
}
