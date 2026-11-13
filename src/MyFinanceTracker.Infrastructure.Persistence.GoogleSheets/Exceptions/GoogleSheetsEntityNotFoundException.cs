namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Exceptions;

internal sealed class GoogleSheetsEntityNotFoundException : Exception
{
    private GoogleSheetsEntityNotFoundException(string message) : base(message) { }

    public static GoogleSheetsEntityNotFoundException TransactionNotFound(string sheetName, Guid transactionId) =>
        new($"Transaction '{transactionId}' was not found in Google Sheet '{sheetName}'.");

    public static GoogleSheetsEntityNotFoundException CategoryNotFound(string sheetName, string categoryId) =>
        new($"Category '{categoryId}' was not found in Google Sheet '{sheetName}'.");
}
