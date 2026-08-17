using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Configuration;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Services;

internal partial class FormulaService(IOptions<GoogleSheetsOptions> options)
{
    [GeneratedRegex(@"[+-]?\d+(?:[.,]\d+)?")]
    private static partial Regex FormulaRegex();

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

    public IReadOnlyList<decimal> Parse(string? formula)
    {
        if (string.IsNullOrWhiteSpace(formula))
        {
            return [];
        }

        var value = formula.Trim().TrimStart('=');
        if (string.IsNullOrEmpty(value) || value == "0")
        {
            return [];
        }

        return [.. FormulaRegex().Matches(value)
            .Select(match => decimal.Parse(match.Value, NumberStyles.Number, numberFormat))];
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