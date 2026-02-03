namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Models;

internal record GoogleSheetBatch(
    string SheetName,
    List<GoogleSheetUpdate> Updates);