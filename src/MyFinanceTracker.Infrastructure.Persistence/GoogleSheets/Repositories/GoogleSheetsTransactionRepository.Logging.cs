using Microsoft.Extensions.Logging;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Logging;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Repositories;

using static LogEvents.Repository;

internal partial class GoogleSheetsTransactionRepository
{
    [LoggerMessage(
        EventId = AddingTransactions,
        Level = LogLevel.Debug,
        Message = "Preparing to add {Count} transactions to Google Sheets")]
    partial void LogAddingTransactions(int count);

    [LoggerMessage(
        EventId = DeletingTransactions,
        Level = LogLevel.Debug,
        Message = "Preparing to delete {Count} transactions from Google Sheets")]
    partial void LogDeletingTransactions(int count);

    [LoggerMessage(
        EventId = UpdatingTransaction,
        Level = LogLevel.Debug,
        Message = "Preparing to update transaction {Id} in Google Sheets")]
    partial void LogUpdatingTransaction(Guid id);

    [LoggerMessage(
        EventId = GettingAllTransactions,
        Level = LogLevel.Debug,
        Message = "Preparing to get all transactions from Google Sheets")]
    partial void LogGettingAllTransactions();
}
