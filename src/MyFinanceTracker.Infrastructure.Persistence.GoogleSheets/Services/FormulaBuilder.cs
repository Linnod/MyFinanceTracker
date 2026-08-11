using System.Globalization;
using Microsoft.Extensions.Options;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Configuration;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Services;

internal class FormulaBuilder(IOptions<GoogleSheetsOptions> options)
{
    private readonly NumberFormatInfo numberFormat = new()
    {
        NumberDecimalSeparator = options.Value.DecimalSeparator
    };

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

    private bool IsZeroOrEmpty(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (value.StartsWith('='))
        {
            return false;
        }

        if (decimal.TryParse(value, NumberStyles.Any, numberFormat, out var number))
        {
            return number == 0;
        }

        return false;
    }
}