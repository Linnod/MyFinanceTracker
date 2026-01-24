using Google.Apis.Sheets.v4.Data;
using MyFinanceTracker.Infrastructure.GoogleSheets.Clients;
using MyFinanceTracker.Infrastructure.GoogleSheets.Models;

namespace MyFinanceTracker.Infrastructure.GoogleSheets.Services;

internal class FormulaService(GoogleSheetsClient client, FormulaBuilder builder)
{
    public async Task<List<ValueRange>> PrepareValueRangesAsync(
        GoogleSheetBatch batch,
        CancellationToken ct)
    {
        var ranges = batch.Updates
            .Select(u => $"{batch.SheetName}!{u.CellAddress}")
            .ToList();
        var currentFormulas = await client.GetFormulasAsync(ranges, ct);

        return [.. batch.Updates.Zip(currentFormulas, (update, currentFormula) => new ValueRange
        {
            Range = $"{batch.SheetName}!{update.CellAddress}",
            Values = [[builder.Merge(currentFormula, update.Delta)]]
        })];
    }
}
