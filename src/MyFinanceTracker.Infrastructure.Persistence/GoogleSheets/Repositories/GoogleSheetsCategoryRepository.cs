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

internal sealed partial class GoogleSheetsCategoryRepository(
    IOptions<GoogleSheetsOptions> options,
    GoogleSheetCategoryMapper mapper,
    IGoogleSheetsClient client,
    ILogger<GoogleSheetsCategoryRepository> logger) : IRemoteCategoryRepository, IDisposable
{
    private readonly GoogleSheetsOptions options = options.Value;
    private readonly SemaphoreSlim semaphore = new(1, 1);

    public void Dispose()
    {
        semaphore.Dispose();
    }

    public async Task<IReadOnlyList<CategorySyncRecord>> GetAll(CancellationToken ct)
    {
        LogGettingAllCategories();

        var validRows = await LoadValidRows(ct);
        return [.. validRows.Select(s => s.Category)];
    }

    public async Task Add(CategorySyncRecord category, CancellationToken ct)
    {
        await semaphore.WaitAsync(ct);
        try
        {
            LogAddingCategory(category.Id);

            var row = mapper.MapToRow(category);
            await client.AppendRows(options.CategoriesSheetName, [row], ct);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task Update(CategorySyncRecord category, CancellationToken ct)
    {
        LogUpdatingCategory(category.Id);

        await semaphore.WaitAsync(ct);
        try
        {
            var validRows = await LoadValidRows(ct);
            var targetRow = validRows.FirstOrDefault(s =>
                s.Category.Id.Equals(category.Id, StringComparison.OrdinalIgnoreCase));

            if (targetRow is null)
            {
                throw GoogleSheetsEntityNotFoundException.CategoryNotFound(options.CategoriesSheetName, category.Id);
            }

            var rowValues = mapper.MapToRow(category);
            await client.UpdateRow(options.CategoriesSheetName, new GoogleSheetRow(targetRow.Row.Index, rowValues), ct);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<IReadOnlyList<CategoryRowMappingResult.Success>> LoadValidRows(CancellationToken ct)
    {
        var allRows = await client.GetRows(options.CategoriesSheetName, ct);
        var dataRows = SkipHeader(allRows);

        var results = mapper.MapFromRows(dataRows);

        var failures = results.OfType<CategoryRowMappingResult.Failure>().ToList();
        ThrowIfFailures(failures);

        var validRows = results.OfType<CategoryRowMappingResult.Success>().ToList();
        ThrowIfDuplicateIds(validRows);

        return validRows;
    }

    private static IEnumerable<GoogleSheetRow> SkipHeader(IEnumerable<GoogleSheetRow> rows)
        => rows.Skip(1);

    private void ThrowIfFailures(List<CategoryRowMappingResult.Failure> failures)
    {
        if (failures.Count > 0)
        {
            throw new GoogleSheetsPersistenceException(options.CategoriesSheetName, failures);
        }
    }

    private void ThrowIfDuplicateIds(List<CategoryRowMappingResult.Success> validRows)
    {
        var duplicateIds = validRows
            .GroupBy(s => s.Category.Id, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateIds.Count > 0)
        {
            throw GoogleSheetsPersistenceException.DuplicateCategories(options.CategoriesSheetName, duplicateIds);
        }
    }
}
