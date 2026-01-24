using System.Globalization;

namespace MyFinanceTracker.Infrastructure.GoogleSheets.Services;

internal class FormulaBuilder
{
    public string Merge(string? currentValue, string delta)
    {
        var baseValue = (currentValue ?? string.Empty).Trim();
        var cleanDelta = delta.Trim();

        if (IsZeroOrEmpty(baseValue))
        {
            return "=" + cleanDelta.TrimStart('+');
        }

        if (baseValue.StartsWith('='))
        {
            return baseValue + cleanDelta;
        }

        return "=" + baseValue + cleanDelta;
    }

    private static bool IsZeroOrEmpty(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (value.StartsWith('='))
        {
            return false;
        }

        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
        {
            return number == 0;
        }

        return false;
    }
}