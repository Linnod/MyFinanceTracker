using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyFinanceTracker.Domain.Common;
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

    public async Task<Transaction?> GetById(Guid id, CancellationToken ct)
    {
        LogGettingTransactionById(id);

        var validRows = await LoadValidRows(ct);
        var targetRow = validRows.FirstOrDefault(s => s.Transaction.Id == id);
        return targetRow?.Transaction;
    }

    public async Task Update(Transaction transaction, CancellationToken ct)
    {
        LogUpdatingTransaction(transaction.Id);

        await semaphore.WaitAsync(ct);
        try
        {
            var validRows = await LoadValidRows(ct);
            var targetRow = validRows.FirstOrDefault(s => s.Transaction.Id == transaction.Id);
            if (targetRow is null)
            {
                throw GoogleSheetsEntityNotFoundException.TransactionNotFound(options.TransactionsSheetName, transaction.Id);
            }

            var rowValues = mapper.MapToRow(transaction);
            await client.UpdateRow(options.TransactionsSheetName, new GoogleSheetRow(targetRow.Row.Index, rowValues), ct);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<IReadOnlyList<Transaction>> GetAll(CancellationToken ct)
    {
        LogGettingAllTransactions();

        var validRows = await LoadValidRows(ct);
        return [.. validRows.Select(s => s.Transaction)];
    }

    public async Task<PagedResult<Transaction>> Get(
        TransactionFilter filter,
        CancellationToken ct)
    {
        LogGettingTransactions(filter);

        var query = (await GetAll(ct)).AsEnumerable();

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
            var categoryIds = filter.Categories.Select(c => c.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            query = query.Where(t => categoryIds.Contains(t.Category.Id));
        }

        if (filter.Type.HasValue)
        {
            query = query.Where(t => t.Type == filter.Type.Value);
        }

        var ordered = query.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id).ToList();
        var totalCount = ordered.Count;

        return new PagedResult<Transaction>(
            [.. ordered.Skip(filter.Skip ?? 0).Take(filter.Take ?? totalCount)],
            totalCount);
    }

    private async Task<IReadOnlyList<RowMappingResult.Success>> LoadValidRows(CancellationToken ct)
    {
        var categories = await categoryRepository.GetAll(ct);
        var allRows = await client.GetRows(options.TransactionsSheetName, ct);
        var dataRows = SkipHeader(allRows);

        var results = mapper.MapToDomain(dataRows, categories);

        var failures = results.OfType<RowMappingResult.Failure>().ToList();
        ThrowIfFailures(failures);

        var validRows = results.OfType<RowMappingResult.Success>().ToList();
        ThrowIfDuplicateIds(validRows);

        return validRows;
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

    private void ThrowIfDuplicateIds(List<RowMappingResult.Success> validRows)
    {
        var duplicateIds = validRows
            .GroupBy(s => s.Transaction.Id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateIds.Count > 0)
        {
            throw GoogleSheetsPersistenceException.DuplicateTransactions(options.TransactionsSheetName, duplicateIds);
        }
    }
}
