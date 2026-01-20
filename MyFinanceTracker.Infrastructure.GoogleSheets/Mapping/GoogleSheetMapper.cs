using System.Globalization;
using Microsoft.Extensions.Options;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Infrastructure.GoogleSheets.Configuration;
using MyFinanceTracker.Infrastructure.GoogleSheets.Models;

namespace MyFinanceTracker.Infrastructure.GoogleSheets.Mapping;

internal class GoogleSheetMapper(IOptions<GoogleSheetsOptions> options)
{
    private readonly GoogleSheetsOptions options = options.Value;
    private const string DateFormat = "yyyy.MM";

    public List<GoogleSheetUpdate> Map(List<Transaction> transactions)
    {
        return [.. transactions
            .GroupBy(t => t.Date)
            .SelectMany(dateGroup => dateGroup
                .GroupBy(t => t.Category)
                .Select(categoryGroup => new GoogleSheetUpdate(
                    SheetName: GetSheetName(dateGroup.Key),
                    CellAddress: GetCellAddress(categoryGroup.Key.Id, dateGroup.Key.Day),
                    Delta: BuildDeltaString([.. categoryGroup])
                ))
            )];
    }

    private static string GetSheetName(DateOnly date) => date.ToString(DateFormat);

    private string GetCellAddress(string columnId, int day) => 
        $"{columnId}{day + options.HeaderRowsCount}";

    private static string BuildDeltaString(List<Transaction> transactions)
    {
        var isIncome = transactions.First().Category.IsIncome;

        return string.Concat(transactions.Select(t =>
        {
            decimal val = isIncome ? t.Amount : -t.Amount;
            return val.ToString("+#;-#;0", CultureInfo.InvariantCulture);
        }));
    }
}
