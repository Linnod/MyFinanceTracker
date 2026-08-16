namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Models;

internal record GoogleSheetCellAddress(
    string SheetName,
    int Row,
    string Column);