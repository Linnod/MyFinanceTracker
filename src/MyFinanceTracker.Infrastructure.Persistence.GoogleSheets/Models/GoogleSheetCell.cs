namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Models;

internal record GoogleSheetCell(
    GoogleSheetCellAddress Address,
    string Content);