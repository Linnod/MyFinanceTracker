using Microsoft.Extensions.Logging;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Models;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Clients;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Mapping;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Services;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Repositories;

internal sealed partial class GoogleSheetsTransactionRepository(
    GoogleSheetMapper mapper,
    IGoogleSheetsClient client,
    FormulaService formulaService,
    ILogger<GoogleSheetsTransactionRepository> logger) : ITransactionRepository
{
    public async Task AddRange(IEnumerable<Transaction> transactions, CancellationToken ct)
    {
        var transactionList = transactions.ToList();
        LogAddingTransactions(transactionList.Count);

        var updates = mapper.MapForAddition(transactionList);
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

    public async Task DeleteRange(Category category, DateOnly date, CancellationToken ct)
    {
        LogDeletingTransactions(category.Name, date);

        var update = mapper.MapForClearance(category.Id, date);
        var batch = new GoogleSheetBatch(update.SheetName, [update]);
        var updateData = formulaService.PrepareForOverwrite(batch);

        await client.SendBatchUpdate(updateData, ct);
    }
}