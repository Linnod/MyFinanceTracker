using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Models;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Clients;

internal interface IGoogleSheetsClient
{
    Task<IReadOnlyList<GoogleSheetCell>> GetCells(IEnumerable<GoogleSheetCellAddress> cellAddresses, CancellationToken ct);
    Task SendBatchUpdate(IEnumerable<GoogleSheetCell> cells, CancellationToken ct);
}
