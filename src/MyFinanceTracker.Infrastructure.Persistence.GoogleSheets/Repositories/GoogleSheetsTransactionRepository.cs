using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Clients;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Configuration;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Exceptions;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Mapping;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Models;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Repositories;

internal sealed partial class GoogleSheetsTransactionRepository(
    IOptions<GoogleSheetsOptions> options,
    GoogleSheetMapper mapper,
    IGoogleSheetsClient client,
    ICategoryRepository categoryRepository,
    ILogger<GoogleSheetsTransactionRepository> logger) : ITransactionRepository
{
    private readonly GoogleSheetsOptions options = options.Value;
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

            var rows = mapper.MapToRows(transactionList);
            await client.AppendRows(options.TransactionsSheetName, rows, ct);
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

            var validRows = await LoadValidRows(ct);

            var rowsToDelete = validRows
                .Where(s => s.Transaction.Date == date &&
                            string.Equals(s.Transaction.Category.Name, category.Name, StringComparison.OrdinalIgnoreCase))
                .Select(s => s.Row)
                .ToList();

            if (rowsToDelete.Count > 0)
            {
                await client.DeleteRows(options.TransactionsSheetName, rowsToDelete, ct);
            }
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

        var validRows = await LoadValidRows(ct);

        return [.. validRows
            .Select(s => s.Transaction)
            .Where(t => t.Date == date && string.Equals(t.Category.Name, category.Name, StringComparison.OrdinalIgnoreCase))];
    }

    private async Task<IReadOnlyList<RowMappingResult.Success>> LoadValidRows(CancellationToken ct)
    {
        var categories = await categoryRepository.GetAll(ct);
        var allRows = await client.GetRows(options.TransactionsSheetName, ct);
        var dataRows = SkipHeader(allRows);

        var results = mapper.MapToDomain(dataRows, categories);

        var failures = results.OfType<RowMappingResult.Failure>().ToList();
        ThrowIfFailures(failures);

        return [.. results.OfType<RowMappingResult.Success>()];
    }

    private static IEnumerable<GoogleSheetRow> SkipHeader(IEnumerable<GoogleSheetRow> rows)
        => rows.Skip(1);

    private void ThrowIfFailures(List<RowMappingResult.Failure> failures)
    {
        if (failures.Count > 0)
        {
            throw new GoogleSheetsPersistenceException(options.TransactionsSheetName, failures);
        }
    }
}
