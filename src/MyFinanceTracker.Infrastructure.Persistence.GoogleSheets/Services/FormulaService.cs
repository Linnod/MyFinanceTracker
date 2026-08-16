using Google.Apis.Sheets.v4.Data;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Clients;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Models;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Services;

internal class FormulaService(IGoogleSheetsClient client, FormulaBuilder builder)
{
    public async Task<List<ValueRange>> PrepareValueRanges(
        IReadOnlyList<GoogleSheetUpdate> updates,
        CancellationToken ct)
    {
        if (updates.Count == 0)
        {
            return [];
        }

        var ranges = updates
            .Select(u => $"{u.SheetName}!{u.CellAddress}")
            .ToList();
        var currentFormulas = await client.GetFormulas(ranges, ct);

        return [.. updates.Zip(currentFormulas, (update, currentFormula) => new ValueRange
        {
            Range = $"{update.SheetName}!{update.CellAddress}",
            Values = [[builder.Merge(currentFormula, update.Content)]]
        })];
    }

    public List<ValueRange> PrepareForOverwrite(GoogleSheetUpdate update)
    {
        return [new ValueRange
        {
            Range = $"{update.SheetName}!{update.CellAddress}",
            Values = [[ update.Content ]]
        }];
    }
}