using Microsoft.Extensions.Logging;
using MyFinanceTracker.Domain.Entities;
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
        Message = "Preparing to delete transactions for category {Category} on {Date}")]
    partial void LogDeletingTransactions(Category category, DateOnly date);

    [LoggerMessage(
    EventId = GettingTransactions,
    Level = LogLevel.Debug,
    Message = "Preparing to get transactions for category {Category} on {Date}")]
    partial void LogGettingTransactions(Category category, DateOnly date);
}