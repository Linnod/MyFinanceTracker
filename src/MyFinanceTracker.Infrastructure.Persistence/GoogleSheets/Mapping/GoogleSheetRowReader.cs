using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Models;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Mapping;

internal static class GoogleSheetRowReader
{
    public static bool IsEmpty(GoogleSheetRowValues rowValues)
        => rowValues.Values.Count == 0 || rowValues.Values.All(string.IsNullOrWhiteSpace);

    public static bool TryGetRequiredValue(GoogleSheetRowValues rowValues, int index, out string value)
    {
        var values = rowValues.Values;
        if (index < values.Count && !string.IsNullOrWhiteSpace(values[index]))
        {
            value = values[index].Trim();
            return true;
        }

        value = string.Empty;
        return false;
    }

    public static string? GetOptionalValue(GoogleSheetRowValues rowValues, int index)
        => TryGetRequiredValue(rowValues, index, out var value) ? value : null;

    public static int ToSheetRowNumber(int rowIndex) => rowIndex + 1;
}
