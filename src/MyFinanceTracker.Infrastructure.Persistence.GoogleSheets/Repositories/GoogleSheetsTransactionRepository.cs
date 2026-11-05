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
    ILogger<GoogleSheetsTransactionRepository> logger) : ITransactionRepository, IDisposable
{
    private readonly GoogleSheetsOptions options = options.Value;
    private readonly SemaphoreSlim semaphore = new(1, 1);

    public void Dispose()
    {
        semaphore.Dispose();
    }

    public async Task AddRange(IReadOnlyCollection<Transaction> transactions, CancellationToken ct)
    {
        if (transactions.Count == 0)
        {
            return;
        }

        await semaphore.WaitAsync(ct);
        try
        {
            LogAddingTransactions(transactions.Count);

            var rows = mapper.MapToRows(transactions);
            await client.AppendRows(options.TransactionsSheetName, rows, ct);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<int> DeleteRange(IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        LogDeletingTransactions(ids.Count);

        if (ids.Count == 0)
        {
            return 0;
        }

        await semaphore.WaitAsync(ct);
        try
        {
            var validRows = await LoadValidRows(ct);
            var idsSet = ids.ToHashSet();

            var rowsToDelete = validRows
                .Where(s => idsSet.Contains(s.Transaction.Id))
                .Select(s => s.Row)
                .ToList();

            if (rowsToDelete.Count > 0)
            {
                await client.DeleteRows(options.TransactionsSheetName, rowsToDelete, ct);
            }

            return rowsToDelete.Count;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<IReadOnlyList<Transaction>> Get(
        TransactionFilter filter,
        CancellationToken ct)
    {
        LogGettingTransactions(filter);

        var validRows = await LoadValidRows(ct);
        var query = validRows.Select(s => s.Transaction);

        if (filter.FromDate.HasValue)
        {
            query = query.Where(t => t.Date >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(t => t.Date <= filter.ToDate.Value);
        }

        if (filter.Categories is { Count: > 0 })
        {
            var categoryNames = filter.Categories.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            query = query.Where(t => categoryNames.Contains(t.Category.Name));
        }

        if (filter.Type.HasValue)
        {
            query = query.Where(t => t.Type == filter.Type.Value);
        }

        return [.. query];
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
