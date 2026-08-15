using Microsoft.Extensions.Logging;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;
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
    private static readonly SemaphoreSlim semaphore = new(1, 1);

    public async Task AddRange(IEnumerable<Transaction> transactions, CancellationToken ct)
    {
        var transactionList = transactions.ToList();
        if (transactionList.Count == 0)
        {
             return;
        }

        await semaphore.WaitAsync(ct);
        try
        {
            LogAddingTransactions(transactionList.Count);

            var updates = mapper.MapForAddition(transactionList);
            var updateData = await formulaService.PrepareValueRanges(updates, ct);
            await client.SendBatchUpdate(updateData, ct);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task DeleteRange(Category category, DateOnly date, CancellationToken ct)
    {
        await semaphore.WaitAsync(ct);
        try
        {
            LogDeletingTransactions(category.Name, date);

            var update = mapper.MapForClearance(category.Id, date);
            var updateData = formulaService.PrepareForOverwrite(update);

            await client.SendBatchUpdate(updateData, ct);
        }
        finally
        {
            semaphore.Release();
        }
    }
}