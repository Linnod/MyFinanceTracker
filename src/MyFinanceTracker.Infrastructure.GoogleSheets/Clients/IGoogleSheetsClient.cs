using Google.Apis.Sheets.v4.Data;

namespace MyFinanceTracker.Infrastructure.GoogleSheets.Clients;

public interface IGoogleSheetsClient
{
    Task<List<string>> GetFormulas(IList<string> ranges, CancellationToken ct);
    Task SendBatchUpdate(List<ValueRange> updateData, CancellationToken ct);
}
