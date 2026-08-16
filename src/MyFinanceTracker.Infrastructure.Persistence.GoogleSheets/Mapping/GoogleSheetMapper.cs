using System.Globalization;
using Microsoft.Extensions.Options;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Configuration;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Models;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Mapping;

internal class GoogleSheetMapper(IOptions<GoogleSheetsOptions> options)
{
    private readonly GoogleSheetsOptions options = options.Value;
    private const string DateFormat = "yyyy.MM";

    public IReadOnlyList<GoogleSheetCell> MapForAddition(IEnumerable<Transaction> transactions)
    {
        var nfi = new NumberFormatInfo
        {
            NumberDecimalSeparator = options.DecimalSeparator
        };

        return [.. transactions
            .GroupBy(t => t.Date)
            .SelectMany(dateGroup => dateGroup
                .GroupBy(t => t.Category)
                .Select(
                    categoryGroup => MapToGoogleSheetCell(categoryGroup.Key, dateGroup.Key, BuildDeltaString([.. categoryGroup], nfi))
                )
            )
        ];
    }

    public GoogleSheetCell MapForClearance(Category category, DateOnly date) => MapToGoogleSheetCell(category, date, "0");

    private GoogleSheetCell MapToGoogleSheetCell(Category category, DateOnly date, string content)
        => new(new GoogleSheetCellAddress(GetSheetName(date), GetCellRow(date.Day), category.Id), content);

    private static string GetSheetName(DateOnly date) => date.ToString(DateFormat);

    private int GetCellRow(int day) => day + options.HeaderRowsCount;

    private static string BuildDeltaString(List<Transaction> transactions, NumberFormatInfo nfi)
    {
        return string.Concat(transactions.Select(t =>
        {
            return t.Amount.ToString("+0.##;-0.##;0", nfi);
        }));
    }
}
