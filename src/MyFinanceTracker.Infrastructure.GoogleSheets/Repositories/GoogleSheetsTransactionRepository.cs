using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.Infrastructure.GoogleSheets.Clients;
using MyFinanceTracker.Infrastructure.GoogleSheets.Mapping;
using MyFinanceTracker.Infrastructure.GoogleSheets.Models;
using MyFinanceTracker.Infrastructure.GoogleSheets.Services;

namespace MyFinanceTracker.Infrastructure.GoogleSheets.Repositories;

internal class GoogleSheetsTransactionRepository(
    GoogleSheetMapper mapper,
    GoogleSheetsClient client,
    FormulaService formulaService) : ITransactionRepository
{
    public async Task AddRange(IEnumerable<Transaction> transactions, CancellationToken ct = default)
    {
        var updates = mapper.Map([.. transactions]);
        var batches = updates
            .GroupBy(u => u.SheetName)
            .Select(g => new GoogleSheetBatch(g.Key, [.. g]));
        foreach (var batch in batches)
        {
            var updateData = await formulaService.PrepareValueRangesAsync(batch, ct);
            await client.SendBatchUpdateAsync(updateData, ct);
        }
    }

    public Task Add(Transaction transaction, CancellationToken ct = default) =>
        AddRange([transaction], ct);
}