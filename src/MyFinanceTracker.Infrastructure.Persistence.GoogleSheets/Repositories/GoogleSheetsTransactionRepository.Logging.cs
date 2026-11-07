using Microsoft.Extensions.Logging;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Logging;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Repositories;

using static LogEvents.Repository;

internal partial class GoogleSheetsTransactionRepository
{
    [LoggerMessage(
        EventId = AddingTransactions,
        Level = LogLevel.Debug,
        Message = "Preparing to add {Count} domain transactions to Google Sheets")]
    partial void LogAddingTransactions(int count);

    [LoggerMessage(
        EventId = DeletingTransactions,
        Level = LogLevel.Debug,
        Message = "Preparing to delete {Count} transactions from Google Sheets")]
    partial void LogDeletingTransactions(int count);

    [LoggerMessage(
        EventId = GettingTransactions,
        Level = LogLevel.Debug,
        Message = "Preparing to get transactions with filter {Filter}")]
    partial void LogGettingTransactions(TransactionFilter filter);

    [LoggerMessage(
        EventId = GettingTransactionById,
        Level = LogLevel.Debug,
        Message = "Preparing to get transaction with ID {Id} from Google Sheets")]
    partial void LogGettingTransactionById(Guid id);

    [LoggerMessage(
        EventId = UpdatingTransaction,
        Level = LogLevel.Debug,
        Message = "Preparing to update transaction {Id} in Google Sheets")]
    partial void LogUpdatingTransaction(Guid id);
}
