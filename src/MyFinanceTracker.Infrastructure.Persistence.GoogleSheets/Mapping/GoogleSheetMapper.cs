using System.Globalization;
using Microsoft.Extensions.Options;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Configuration;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Models;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Mapping;

internal class GoogleSheetMapper(IOptions<GoogleSheetsOptions> options)
{
    private const NumberStyles AmountNumberStyles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    private const string WriteDateFormat = "yyyy-MM-dd";

    private static readonly string[] SupportedDateFormats =
    [
        WriteDateFormat,
        "dd.MM.yyyy",
        "d.M.yyyy",
        "dd/MM/yyyy",
        "yyyy.MM.dd"
    ];

    private static class Columns
    {
        public const int Id = 0;
        public const int Date = 1;
        public const int Type = 2;
        public const int Category = 3;
        public const int Amount = 4;
        public const int Note = 5;
    }

    private readonly NumberFormatInfo numberFormatInfo = new()
    {
        NumberDecimalSeparator = options.Value.DecimalSeparator
    };

    public GoogleSheetRowValues MapToRow(Transaction transaction)
    {
        var signedAmount = transaction.Type == TransactionType.Expense
            ? -transaction.Amount
            : transaction.Amount;

        return new GoogleSheetRowValues([
            transaction.Id.ToString(),
            transaction.Date.ToString(WriteDateFormat, CultureInfo.InvariantCulture),
            transaction.Type.ToString(),
            transaction.Category.Name,
            signedAmount.ToString("0.##", numberFormatInfo),
            transaction.Note ?? string.Empty
        ]);
    }

    public IReadOnlyList<GoogleSheetRowValues> MapToRows(IEnumerable<Transaction> transactions)
        => [.. transactions.Select(MapToRow)];

    public RowMappingResult MapToDomain(GoogleSheetRow row, IReadOnlyCollection<Category> categories)
        => MapToDomain(row, CreateCategoryLookup(categories));

    public IReadOnlyList<RowMappingResult> MapToDomain(
        IEnumerable<GoogleSheetRow> rows,
        IReadOnlyCollection<Category> categories)
    {
        var categoriesByName = CreateCategoryLookup(categories);
        return [.. rows.Select(r => MapToDomain(r, categoriesByName))];
    }

    private static Dictionary<string, Category> CreateCategoryLookup(IReadOnlyCollection<Category> categories)
        => categories.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

    private RowMappingResult MapToDomain(
        GoogleSheetRow row,
        Dictionary<string, Category> categoriesByName)
    {
        var rowNumber = ToSheetRowNumber(row.Index);
        var rowValues = row.Values;

        if (rowValues.Values.Count == 0 || rowValues.Values.All(string.IsNullOrWhiteSpace))
        {
            return new RowMappingResult.Empty();
        }

        if (!TryGetRequiredValue(rowValues.Values, Columns.Id, out var idString))
        {
            return new RowMappingResult.Failure(rowNumber, "Missing required 'Id' value.");
        }

        if (!Guid.TryParse(idString, out var id))
        {
            return new RowMappingResult.Failure(rowNumber, $"Invalid Transaction ID: '{idString}'.");
        }

        if (!TryGetRequiredValue(rowValues.Values, Columns.Date, out var dateString))
        {
            return new RowMappingResult.Failure(rowNumber, "Missing required 'Date' value.");
        }

        if (!DateOnly.TryParseExact(dateString, SupportedDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return new RowMappingResult.Failure(rowNumber, $"Invalid Date: '{dateString}'.");
        }

        if (!TryGetRequiredValue(rowValues.Values, Columns.Type, out var typeString))
        {
            return new RowMappingResult.Failure(rowNumber, "Missing required 'Type' value.");
        }

        if (!Enum.TryParse<TransactionType>(typeString, ignoreCase: true, out var type))
        {
            return new RowMappingResult.Failure(rowNumber, $"Invalid Transaction Type: '{typeString}'. Expected 'Income' or 'Expense'.");
        }

        if (!TryGetRequiredValue(rowValues.Values, Columns.Category, out var categoryName))
        {
            return new RowMappingResult.Failure(rowNumber, "Missing required 'Category' value.");
        }

        if (!categoriesByName.TryGetValue(categoryName, out var category))
        {
            return new RowMappingResult.Failure(rowNumber, $"Unknown Category: '{categoryName}'.");
        }

        if (!TryGetRequiredValue(rowValues.Values, Columns.Amount, out var amountString))
        {
            return new RowMappingResult.Failure(rowNumber, "Missing required 'Amount' value.");
        }

        if (!TryParseAmount(amountString, out var rawAmount) || rawAmount == 0)
        {
            return new RowMappingResult.Failure(rowNumber, $"Invalid Amount: '{amountString}'.");
        }

        var amount = Math.Abs(rawAmount);
        var note = GetOptionalValue(rowValues.Values, Columns.Note);

        return new RowMappingResult.Success(row, new Transaction(id, type, category, amount, date, note));
    }

    private static bool TryGetRequiredValue(IReadOnlyList<string> values, int index, out string value)
    {
        if (index < values.Count && !string.IsNullOrWhiteSpace(values[index]))
        {
            value = values[index].Trim();
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static string? GetOptionalValue(IReadOnlyList<string> values, int index)
    {
        if (index >= values.Count || string.IsNullOrWhiteSpace(values[index]))
        {
            return null;
        }

        return values[index].Trim();
    }

    private bool TryParseAmount(string amountString, out decimal amount)
        => decimal.TryParse(amountString, AmountNumberStyles, numberFormatInfo, out amount);

    private static int ToSheetRowNumber(int rowIndex) => rowIndex + 1;
}
