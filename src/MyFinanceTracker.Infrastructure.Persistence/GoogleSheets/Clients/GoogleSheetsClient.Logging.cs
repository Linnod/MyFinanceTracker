using Microsoft.Extensions.Logging;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Logging;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Clients;

using static LogEvents.Client;

internal partial class GoogleSheetsClient
{
    [LoggerMessage(
        EventId = FetchingRows,
        Level = LogLevel.Debug,
        Message = "Fetching rows from sheet '{SheetName}' in spreadsheet '{SpreadsheetId}'")]
    partial void LogFetchingRows(string sheetName, string spreadsheetId);

    [LoggerMessage(
        EventId = RowsFetched,
        Level = LogLevel.Debug,
        Message = "Successfully fetched {RowCount} rows from sheet '{SheetName}'")]
    partial void LogRowsFetched(int rowCount, string sheetName);

    [LoggerMessage(
        EventId = AppendingRows,
        Level = LogLevel.Information,
        Message = "Appending {RowCount} rows to sheet '{SheetName}' in spreadsheet '{SpreadsheetId}'")]
    partial void LogAppendingRows(int rowCount, string sheetName, string spreadsheetId);

    [LoggerMessage(
        EventId = RowsAppended,
        Level = LogLevel.Debug,
        Message = "Successfully appended rows to sheet '{SheetName}'")]
    partial void LogRowsAppended(string sheetName);

    [LoggerMessage(
        EventId = DeletingRows,
        Level = LogLevel.Information,
        Message = "Deleting {RowCount} rows from sheet '{SheetName}' in spreadsheet '{SpreadsheetId}'")]
    partial void LogDeletingRows(int rowCount, string sheetName, string spreadsheetId);

    [LoggerMessage(
        EventId = RowsDeleted,
        Level = LogLevel.Debug,
        Message = "Successfully deleted rows from sheet '{SheetName}'")]
    partial void LogRowsDeleted(string sheetName);

    [LoggerMessage(
        EventId = UpdatingRow,
        Level = LogLevel.Information,
        Message = "Updating row at index {RowIndex} in sheet '{SheetName}' in spreadsheet '{SpreadsheetId}'")]
    partial void LogUpdatingRow(int rowIndex, string sheetName, string spreadsheetId);

    [LoggerMessage(
        EventId = RowUpdated,
        Level = LogLevel.Debug,
        Message = "Successfully updated row at index {RowIndex} in sheet '{SheetName}'")]
    partial void LogRowUpdated(int rowIndex, string sheetName);
}
