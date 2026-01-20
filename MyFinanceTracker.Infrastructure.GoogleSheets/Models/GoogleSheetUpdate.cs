namespace MyFinanceTracker.Infrastructure.GoogleSheets.Models;

internal record GoogleSheetUpdate(
    string SheetName,
    string CellAddress,
    string Delta);