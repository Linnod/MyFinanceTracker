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

    public List<GoogleSheetUpdate> MapForAddition(List<Transaction> transactions)
    {
        var nfi = new NumberFormatInfo
        {
            NumberDecimalSeparator = options.DecimalSeparator
        };

        return [.. transactions
            .GroupBy(t => t.Date)
            .SelectMany(dateGroup => dateGroup
                .GroupBy(t => t.Category)
                .Select(categoryGroup => new GoogleSheetUpdate(
                    SheetName: GetSheetName(dateGroup.Key),
                    CellAddress: GetCellAddress(categoryGroup.Key.Id, dateGroup.Key.Day),
                    Content: BuildDeltaString([.. categoryGroup], nfi)
                ))
            )];
    }

    public GoogleSheetUpdate MapForClearance(string columnId, DateOnly date)
    {
        return new GoogleSheetUpdate(
            SheetName: GetSheetName(date),
            CellAddress: GetCellAddress(columnId, date.Day),
            Content: "0"
        );
    }

    private static string GetSheetName(DateOnly date) => date.ToString(DateFormat);

    private string GetCellAddress(string columnId, int day) =>
        $"{columnId}{day + options.HeaderRowsCount}";

    private static string BuildDeltaString(List<Transaction> transactions, NumberFormatInfo nfi)
    {
        var isIncome = transactions.First().Category.IsIncome;

        return string.Concat(transactions.Select(t =>
        {
            decimal val = isIncome ? t.Amount : -t.Amount;

            return val.ToString("+0.##;-0.##;0", nfi);
        }));
    }
}
