namespace MyFinanceTracker.Infrastructure.GoogleSheets.Models;

internal record GoogleSheetBatch(
    string SheetName,
    List<GoogleSheetUpdate> Updates);