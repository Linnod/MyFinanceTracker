using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Clients;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Models;

namespace MyFinanceTracker.Integration.Tests.Fixtures;

internal sealed class InMemoryGoogleSheetsClient : IGoogleSheetsClient
{
    public const string CategoriesSheetName = "Categories";
    public const string TransactionsSheetName = "Transactions";

    private readonly Lock syncLock = new();
    private readonly Dictionary<string, List<GoogleSheetRowValues>> sheets;

    public InMemoryGoogleSheetsClient()
    {
        sheets = new Dictionary<string, List<GoogleSheetRowValues>>(StringComparer.OrdinalIgnoreCase)
        {
            [CategoriesSheetName] = [new GoogleSheetRowValues(["Id", "Name", "IsArchived"])],
            [TransactionsSheetName] = [new GoogleSheetRowValues(["Id", "Date", "Type", "Category", "Amount", "Note"])]
        };
    }

    public void SeedCategory(string id, string name, bool isArchived = false)
    {
        lock (syncLock)
        {
            GetSheet(CategoriesSheetName).Add(new GoogleSheetRowValues([id, name, isArchived ? "TRUE" : "FALSE"]));
        }
    }

    public IReadOnlyList<GoogleSheetRow> GetSheetRows(string sheetName)
    {
        lock (syncLock)
        {
            return SnapshotRows(GetSheet(sheetName));
        }
    }

    public IReadOnlyList<GoogleSheetRow> GetDataRows(string sheetName)
    {
        lock (syncLock)
        {
            return [.. SnapshotRows(GetSheet(sheetName)).Skip(1)];
        }
    }

    public Task<IReadOnlyList<GoogleSheetRow>> GetRows(string sheetName, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        lock (syncLock)
        {
            return Task.FromResult<IReadOnlyList<GoogleSheetRow>>(SnapshotRows(GetSheet(sheetName)));
        }
    }

    public Task AppendRows(string sheetName, IEnumerable<GoogleSheetRowValues> rows, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var rowsList = rows.ToList();
        if (rowsList.Count == 0)
        {
            return Task.CompletedTask;
        }

        lock (syncLock)
        {
            var sheet = GetSheet(sheetName);
            foreach (var row in rowsList)
            {
                sheet.Add(new GoogleSheetRowValues([.. row.Values]));
            }
        }

        return Task.CompletedTask;
    }

    public Task DeleteRows(string sheetName, IReadOnlyCollection<GoogleSheetRow> rows, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (rows.Count == 0)
        {
            return Task.CompletedTask;
        }

        lock (syncLock)
        {
            var sheet = GetSheet(sheetName);
            var indicesToDelete = rows
                .Select(r => r.Index)
                .Distinct()
                .OrderByDescending(i => i)
                .ToList();

            foreach (var index in indicesToDelete)
            {
                if (index < 0 || index >= sheet.Count)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(rows),
                        $"Row index {index} is out of range for sheet '{sheetName}'.");
                }
            }

            foreach (var index in indicesToDelete)
            {
                sheet.RemoveAt(index);
            }
        }

        return Task.CompletedTask;
    }

    public Task UpdateRow(string sheetName, GoogleSheetRow row, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        lock (syncLock)
        {
            var sheet = GetSheet(sheetName);
            if (row.Index < 0 || row.Index >= sheet.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(row),
                    $"Row index {row.Index} is out of range for sheet '{sheetName}'.");
            }

            sheet[row.Index] = new GoogleSheetRowValues([.. row.Values.Values]);
        }

        return Task.CompletedTask;
    }

    private List<GoogleSheetRowValues> GetSheet(string sheetName) =>
        sheets.TryGetValue(sheetName, out var sheet)
            ? sheet
            : throw new InvalidOperationException($"Sheet '{sheetName}' does not exist.");

    private static List<GoogleSheetRow> SnapshotRows(List<GoogleSheetRowValues> sheet) =>
        [.. sheet.Select((values, index) => new GoogleSheetRow(index, values))];
}
