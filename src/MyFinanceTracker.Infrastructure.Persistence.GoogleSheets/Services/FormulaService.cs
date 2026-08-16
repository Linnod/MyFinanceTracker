using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Clients;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Models;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Services;

internal class FormulaService(IGoogleSheetsClient client, FormulaBuilder builder)
{
    public async Task<List<GoogleSheetCell>> PrepareCellsForUpdate(
        IReadOnlyList<GoogleSheetCell> updates,
        CancellationToken ct)
    {
        if (updates.Count == 0)
        {
            return [];
        }

        var currentCells = await client.GetCells(
            updates.Select(c => c.Address),
            ct);

        return [.. updates.Zip(
            currentCells,
            (update, current) => new GoogleSheetCell(
                update.Address,
                builder.Merge(current.Content, update.Content)))];
    }
}