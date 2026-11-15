using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Models;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Clients;

internal interface IGoogleSheetsClient
{
    Task<IReadOnlyList<GoogleSheetRow>> GetRows(string sheetName, CancellationToken ct);
    Task AppendRows(string sheetName, IEnumerable<GoogleSheetRowValues> rows, CancellationToken ct);
    Task DeleteRows(string sheetName, IReadOnlyCollection<GoogleSheetRow> rows, CancellationToken ct);
    Task UpdateRow(string sheetName, GoogleSheetRow row, CancellationToken ct);
}
