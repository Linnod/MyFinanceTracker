using System.Globalization;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Models;
using MyFinanceTracker.Infrastructure.Persistence.Models;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Mapping;

internal sealed class GoogleSheetTransactionMapper
{
    private const NumberStyles AmountNumberStyles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    private const string DateFormat = "yyyy-MM-dd HH:mm:ss";

    private static class Columns
    {
        public const int Id = 0;
        public const int Date = 1;
        public const int Type = 2;
        public const int Category = 3;
        public const int Amount = 4;
        public const int Note = 5;
    }

    public GoogleSheetRowValues MapToRow(TransactionSyncRecord transaction)
    {
        var signedAmount = transaction.Type == TransactionType.Expense
            ? -transaction.Amount
            : transaction.Amount;

        return new GoogleSheetRowValues([
            transaction.Id.ToString(),
            transaction.Date.ToString(DateFormat, CultureInfo.InvariantCulture),
            transaction.Type.ToString(),
            transaction.CategoryId,
            signedAmount.ToString("0.##", CultureInfo.InvariantCulture),
            transaction.Note ?? string.Empty
        ]);
    }

    public IReadOnlyList<GoogleSheetRowValues> MapToRows(IEnumerable<TransactionSyncRecord> transactions)
        => [.. transactions.Select(MapToRow)];

    public IReadOnlyList<TransactionRowMappingResult> MapFromRows(IEnumerable<GoogleSheetRow> rows)
        => [.. rows.Select(ParseRow)];

    private static TransactionRowMappingResult ParseRow(GoogleSheetRow row)
    {
        var rowNumber = GoogleSheetRowReader.ToSheetRowNumber(row.Index);
        var rowValues = row.Values;

        if (GoogleSheetRowReader.IsEmpty(rowValues))
        {
            return new TransactionRowMappingResult.Empty();
        }

        if (!GoogleSheetRowReader.TryGetRequiredValue(rowValues, Columns.Id, out var idString))
        {
            return new TransactionRowMappingResult.Failure(rowNumber, "Missing required 'Id' value.");
        }

        if (!Guid.TryParse(idString, out var id) || id == Guid.Empty)
        {
            return new TransactionRowMappingResult.Failure(rowNumber, $"Invalid Transaction ID: '{idString}'.");
        }

        if (!GoogleSheetRowReader.TryGetRequiredValue(rowValues, Columns.Date, out var dateString))
        {
            return new TransactionRowMappingResult.Failure(rowNumber, "Missing required 'Date' value.");
        }

        if (!DateTime.TryParseExact(dateString, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return new TransactionRowMappingResult.Failure(rowNumber, $"Invalid Date: '{dateString}'.");
        }

        if (date.Year < FinancialRules.MinAllowedYear || date.Year > FinancialRules.MaxAllowedYear)
        {
            return new TransactionRowMappingResult.Failure(
                rowNumber,
                $"Invalid Date: '{dateString}'. Year must be between {FinancialRules.MinAllowedYear} and {FinancialRules.MaxAllowedYear}.");
        }

        if (!GoogleSheetRowReader.TryGetRequiredValue(rowValues, Columns.Type, out var typeString))
        {
            return new TransactionRowMappingResult.Failure(rowNumber, "Missing required 'Type' value.");
        }

        if (!TryParseTransactionType(typeString, out var type))
        {
            return new TransactionRowMappingResult.Failure(rowNumber, $"Invalid Transaction Type: '{typeString}'. Expected 'Income' or 'Expense'.");
        }

        if (!GoogleSheetRowReader.TryGetRequiredValue(rowValues, Columns.Category, out var categoryId))
        {
            return new TransactionRowMappingResult.Failure(rowNumber, "Missing required 'Category' value.");
        }

        if (!GoogleSheetRowReader.TryGetRequiredValue(rowValues, Columns.Amount, out var amountString))
        {
            return new TransactionRowMappingResult.Failure(rowNumber, "Missing required 'Amount' value.");
        }

        if (!TryParseAmount(amountString, out var rawAmount)
            || rawAmount == 0
            || !FinancialRules.HasValidAmountPrecision(rawAmount))
        {
            return new TransactionRowMappingResult.Failure(rowNumber, $"Invalid Amount: '{amountString}'.");
        }

        var amount = Math.Abs(rawAmount);
        var note = GoogleSheetRowReader.GetOptionalValue(rowValues, Columns.Note);

        return new TransactionRowMappingResult.Success(row, new TransactionSyncRecord(id, type, categoryId, amount, date, note));
    }

    private static bool TryParseAmount(string amountString, out decimal amount)
        => decimal.TryParse(amountString, AmountNumberStyles, CultureInfo.InvariantCulture, out amount);

    private static bool TryParseTransactionType(string typeString, out TransactionType type)
    {
        if (Enum.TryParse(typeString, ignoreCase: true, out type)
            && Enum.IsDefined(type)
            && string.Equals(typeString, type.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        type = default;
        return false;
    }
}
