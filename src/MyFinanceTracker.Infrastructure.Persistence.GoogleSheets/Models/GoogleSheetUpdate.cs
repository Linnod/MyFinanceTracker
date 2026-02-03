namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Models;

internal record GoogleSheetUpdate(
    string SheetName,
    string CellAddress,
    string Content);