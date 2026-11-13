using MyFinanceTracker.Domain.Exceptions;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Mapping;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Exceptions;

internal sealed class GoogleSheetsPersistenceException : DataIntegrityException
{
    public IReadOnlyList<RowMappingResult.Failure> Failures { get; }

    public GoogleSheetsPersistenceException(string sheetName, IReadOnlyList<RowMappingResult.Failure> failures)
        : base(FormatMessage(sheetName, failures.Select(f => (f.RowNumber, f.Reason))))
    {
        Failures = failures;
    }

    public GoogleSheetsPersistenceException(string sheetName, IReadOnlyList<CategoryRowMappingResult.Failure> failures)
        : base(FormatMessage(sheetName, failures.Select(f => (f.RowNumber, f.Reason))))
    {
        Failures = [.. failures.Select(f => new RowMappingResult.Failure(f.RowNumber, f.Reason))];
    }

    private GoogleSheetsPersistenceException(string message) : base(message)
    {
        Failures = [];
    }

    public static GoogleSheetsPersistenceException DuplicateCategories(string sheetName, IEnumerable<string> duplicateIds) =>
        new($"Duplicate category IDs found in Google Sheet '{sheetName}': {string.Join(", ", duplicateIds)}.");

    public static GoogleSheetsPersistenceException DuplicateTransactions(string sheetName, IEnumerable<Guid> duplicateIds) =>
        new($"Duplicate transaction IDs found in Google Sheet '{sheetName}': {string.Join(", ", duplicateIds)}.");

    private static string FormatMessage(string sheetName, IEnumerable<(int RowNumber, string Reason)> failures)
    {
        var errors = string.Join(Environment.NewLine, failures.Select(f => $"Row {f.RowNumber}: {f.Reason}"));
        return $"Errors found in Google Sheet '{sheetName}':{Environment.NewLine}{errors}";
    }
}
