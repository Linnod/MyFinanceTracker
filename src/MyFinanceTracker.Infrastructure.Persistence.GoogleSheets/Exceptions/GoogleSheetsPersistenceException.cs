using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Mapping;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Exceptions;

internal sealed class GoogleSheetsPersistenceException : Exception
{
    public IReadOnlyList<RowMappingResult.Failure> Failures { get; }

    public GoogleSheetsPersistenceException(string sheetName, IReadOnlyList<RowMappingResult.Failure> failures)
        : base(FormatMessage(sheetName, failures))
    {
        Failures = failures;
    }

    private GoogleSheetsPersistenceException(string message) : base(message)
    {
        Failures = [];
    }

    public static GoogleSheetsPersistenceException TransactionNotFound(string sheetName, Guid transactionId) =>
        new($"Transaction '{transactionId}' was not found in Google Sheet '{sheetName}'.");

    private static string FormatMessage(string sheetName, IReadOnlyList<RowMappingResult.Failure> failures)
    {
        var errors = string.Join(Environment.NewLine, failures.Select(f => $"Row {f.RowNumber}: {f.Reason}"));
        return $"Errors found in Google Sheet '{sheetName}':{Environment.NewLine}{errors}";
    }
}
