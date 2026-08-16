using Microsoft.Extensions.Logging;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Logging;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Clients;

using static LogEvents.Client;

internal partial class GoogleSheetsClient
{
    [LoggerMessage(
        EventId = FetchingFormulas,
        Level = LogLevel.Debug,
        Message = "Fetching formulas for {CellCount} ranges from spreadsheet '{SpreadsheetId}'")]
    partial void LogFetchingCells(int cellCount, string spreadsheetId);

    [LoggerMessage(
        EventId = FormulasFetched,
        Level = LogLevel.Debug,
        Message = "Successfully fetched {CellCount} value ranges")]
    partial void LogCellsFetched(int cellCount);

    [LoggerMessage(
        EventId = SendingBatchUpdate,
        Level = LogLevel.Information,
        Message = "Sending batch update to Google Sheets ({UpdateCount} entries) for spreadsheet '{SpreadsheetId}'")]
    partial void LogSendingBatchUpdate(int updateCount, string spreadsheetId);

    [LoggerMessage(
        EventId = BatchUpdateApplied,
        Level = LogLevel.Debug,
        Message = "Batch update applied successfully")]
    partial void LogBatchUpdateApplied();
}