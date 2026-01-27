using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.Infrastructure.GoogleSheets.Clients;
using MyFinanceTracker.Infrastructure.GoogleSheets.Mapping;
using MyFinanceTracker.Infrastructure.GoogleSheets.Models;
using MyFinanceTracker.Infrastructure.GoogleSheets.Services;

namespace MyFinanceTracker.Infrastructure.GoogleSheets.Repositories;

internal class GoogleSheetsTransactionRepository(
    GoogleSheetMapper mapper,
    IGoogleSheetsClient client,
    FormulaService formulaService) : ITransactionRepository
{
    public async Task AddRange(IEnumerable<Transaction> transactions, CancellationToken ct = default)
    {
        var updates = mapper.MapForAddition([.. transactions]);
        var batches = updates
            .GroupBy(u => u.SheetName)
            .Select(g => new GoogleSheetBatch(g.Key, [.. g]));
        var tasks = batches.Select(async batch =>
        {
            var updateData = await formulaService.PrepareValueRangesAsync(batch, ct);
            await client.SendBatchUpdate(updateData, ct);
        });
        await Task.WhenAll(tasks);
    }

    public async Task DeleteRange(Category category, DateOnly date, CancellationToken ct = default)
    {
        var update = mapper.MapForClearance(category.Id, date);
        var batch = new GoogleSheetBatch(update.SheetName, [update]);
        var updateData = formulaService.PrepareForOverwrite(batch);
        await client.SendBatchUpdate(updateData, ct);
    }
}