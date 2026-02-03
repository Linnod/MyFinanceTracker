using Google.Apis.Sheets.v4.Data;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Clients;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Models;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Services;

internal class FormulaService(IGoogleSheetsClient client, FormulaBuilder builder)
{
    public async Task<List<ValueRange>> PrepareValueRangesAsync(
        GoogleSheetBatch batch,
        CancellationToken ct)
    {
        var ranges = batch.Updates
            .Select(u => $"{batch.SheetName}!{u.CellAddress}")
            .ToList();
        var currentFormulas = await client.GetFormulas(ranges, ct);

        return [.. batch.Updates.Zip(currentFormulas, (update, currentFormula) => new ValueRange
        {
            Range = $"{batch.SheetName}!{update.CellAddress}",
            Values = [[builder.Merge(currentFormula, update.Content)]]
        })];
    }

    public List<ValueRange> PrepareForOverwrite(GoogleSheetBatch batch)
    {

        return [.. batch.Updates.Select(u => new ValueRange
        {
            Range = $"{batch.SheetName}!{u.CellAddress}",
            Values = [[ u.Content ]]
        })];
    }
}
