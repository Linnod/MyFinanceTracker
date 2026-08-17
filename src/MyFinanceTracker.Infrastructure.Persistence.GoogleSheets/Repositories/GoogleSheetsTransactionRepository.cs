using Microsoft.Extensions.Logging;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Clients;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Mapping;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Models;
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
            var currentCells = await client.GetCells(updates.Select(c => c.Address), ct);
            var cells = updates.Zip(
                currentCells,
                (update, current) => new GoogleSheetCell(
                    update.Address,
                    formulaService.Merge(current.Content, update.Content)));
            await client.SendBatchUpdate(cells, ct);
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
            LogDeletingTransactions(category, date);

            var update = mapper.MapForClearance(category, date);
            await client.SendBatchUpdate([update], ct);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<IReadOnlyList<Transaction>> Get(
        Category category,
        DateOnly date,
        CancellationToken ct)
    {
        LogGettingTransactions(category, date);

        var cellAddress = mapper.MapForRead(category, date);
        var cell = (await client.GetCells([cellAddress], ct)).SingleOrDefault();

        return [.. formulaService
            .Parse(cell?.Content)
            .Select(amount => new Transaction(
                Guid.NewGuid(),
                category,
                amount,
                date,
                null))];
    }
}