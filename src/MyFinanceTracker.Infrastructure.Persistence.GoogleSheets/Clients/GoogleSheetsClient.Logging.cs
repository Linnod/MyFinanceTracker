using Microsoft.Extensions.Logging;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Logging;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Clients;

using static LogEvents.Client;

internal partial class GoogleSheetsClient
{
    [LoggerMessage(
        EventId = FetchingFormulas,
        Level = LogLevel.Debug,
        Message = "Fetching formulas for {Count} ranges from spreadsheet {Id}")]
    partial void LogFetchingFormulas(int count, string id);

    [LoggerMessage(
        EventId = FormulasFetched,
        Level = LogLevel.Debug,
        Message = "Successfully fetched {Count} value ranges")]
    partial void LogFormulasFetched(int count);

    [LoggerMessage(
        EventId = SendingBatchUpdate,
        Level = LogLevel.Information,
        Message = "Sending batch update to Google Sheets ({Count} entries) for spreadsheet {Id}")]
    partial void LogSendingBatchUpdate(int count, string id);

    [LoggerMessage(
        EventId = BatchUpdateApplied,
        Level = LogLevel.Debug,
        Message =  "Batch update applied successfully")]
    partial void LogBatchUpdateApplied();
}