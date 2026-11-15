using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Clients;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Configuration;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Exceptions;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Mapping;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Models;
using MyFinanceTracker.Infrastructure.Persistence.Models;
using MyFinanceTracker.Infrastructure.Persistence.Repositories;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Repositories;

internal sealed partial class GoogleSheetsTransactionRepository(
    IOptions<GoogleSheetsOptions> options,
    GoogleSheetTransactionMapper mapper,
    IGoogleSheetsClient client,
    ILogger<GoogleSheetsTransactionRepository> logger) : IRemoteTransactionRepository, IDisposable
{
    private readonly GoogleSheetsOptions options = options.Value;
    private readonly SemaphoreSlim semaphore = new(1, 1);

    public void Dispose()
    {
        semaphore.Dispose();
    }

    public async Task AddRange(IReadOnlyCollection<TransactionSyncRecord> transactions, CancellationToken ct)
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
                .Where(r => idsSet.Contains(r.Transaction.Id))
                .Select(r => r.Row)
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

    public async Task Update(TransactionSyncRecord transaction, CancellationToken ct)
    {
        LogUpdatingTransaction(transaction.Id);

        await semaphore.WaitAsync(ct);
        try
        {
            var validRows = await LoadValidRows(ct);
            var targetRow = validRows
                .Where(r => r.Transaction.Id == transaction.Id)
                .Select(r => r.Row)
                .FirstOrDefault();

            if (targetRow is null)
            {
                throw GoogleSheetsEntityNotFoundException.TransactionNotFound(options.TransactionsSheetName, transaction.Id);
            }

            var rowValues = mapper.MapToRow(transaction);
            await client.UpdateRow(options.TransactionsSheetName, new GoogleSheetRow(targetRow.Index, rowValues), ct);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<IReadOnlyList<TransactionSyncRecord>> GetAll(CancellationToken ct)
    {
        LogGettingAllTransactions();

        var validRows = await LoadValidRows(ct);
        return [.. validRows.Select(s => s.Transaction)];
    }

    private async Task<IReadOnlyList<TransactionRowMappingResult.Success>> LoadValidRows(CancellationToken ct)
    {
        var allRows = await client.GetRows(options.TransactionsSheetName, ct);
        var dataRows = SkipHeader(allRows);

        var results = mapper.MapFromRows(dataRows);

        var failures = results.OfType<TransactionRowMappingResult.Failure>().ToList();
        ThrowIfFailures(failures);

        var validRows = results.OfType<TransactionRowMappingResult.Success>().ToList();
        ThrowIfDuplicateIds(validRows.Select(s => s.Transaction.Id));

        return validRows;
    }

    private static IEnumerable<GoogleSheetRow> SkipHeader(IEnumerable<GoogleSheetRow> rows)
        => rows.Skip(1);

    private void ThrowIfFailures(IReadOnlyList<TransactionRowMappingResult.Failure> failures)
    {
        if (failures.Count > 0)
        {
            throw new GoogleSheetsPersistenceException(options.TransactionsSheetName, failures);
        }
    }

    private void ThrowIfDuplicateIds(IEnumerable<Guid> ids)
    {
        var duplicateIds = ids
            .GroupBy(id => id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateIds.Count > 0)
        {
            throw GoogleSheetsPersistenceException.DuplicateTransactions(options.TransactionsSheetName, duplicateIds);
        }
    }
}
