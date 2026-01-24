using System.Globalization;
using System.Text.RegularExpressions;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Interactions.Parsing.Models;
using MyFinanceTracker.Interactions.Parsing.Parser.Exceptions;

namespace MyFinanceTracker.Interactions.Parsing.Parser;

internal partial class FinancialOperationParser : IFinancialOperationParser
{
    [GeneratedRegex(@"^\s*(?:(?<type>income|return|adjust|expense)\s+)?(?:(?<category>[a-zA-Zа-яА-ЯёЁ_][^\d\s]*)\s+)?(?<amounts>\d+[.,]?\d*(?!\d+[.,]\d+[.,])(?:\s+(?!\d+[.,]\d+[.,])\d+[.,]?\d*)*)(?:\s+(?<date>\d{1,2}\.\d{1,2}\.\d{2,4}))?\s*(?<notes>.*)?$", RegexOptions.IgnoreCase)]
    private static partial Regex CommandRegex();

    public RawFinancialOperation Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw ParsingException.EmptyInput();
        }

        var match = CommandRegex().Match(input.Trim());
        if (!match.Success)
        {
            return new RawFinancialOperation(null, null, [], null, string.Empty);
        }

        return new RawFinancialOperation(
            Type: ExtractType(match.Groups["type"].Value.ToLower()),
            CategoryAlias: ExtractCategoryAlias(match.Groups["category"].Value),
            Amounts: ParseAmounts(match.Groups["amounts"].Value),
            Date: ParseDate(match.Groups["date"].Value),
            Notes: match.Groups["notes"].Value.Trim()
        );
    }

    private static string? ExtractCategoryAlias(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value;
    }

    private static FinancialOperationType? ExtractType(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.ToLower() switch
        {
            "income" => FinancialOperationType.Income,
            "return" => FinancialOperationType.Return,
            "adjust" => FinancialOperationType.Adjustment,
            "expense" => FinancialOperationType.Expense,
            _ => null
        };
    }

    private static decimal[] ParseAmounts(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        var tokens = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var result = new decimal[tokens.Length];

        for (int i = 0; i < tokens.Length; i++)
        {
            var clean = tokens[i].Replace(',', '.');

            if (!decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
            {
                throw ParsingException.InvalidAmount(tokens[i]);
            }

            result[i] = val;
        }

        return result;
    }

    private static DateOnly? ParseDate(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        string[] formats = ["dd.MM.yyyy", "dd.MM.yy", "d.M.yyyy", "d.M.yy"];
        if (!DateOnly.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            throw ParsingException.InvalidDate(value);
        }

        if (date.Year < FinancialRules.MinAllowedYear)
        {
            throw ParsingException.InvalidDateRange(date);
        }

        if (date.Year > FinancialRules.MaxAllowedYear)
        {
            throw ParsingException.InvalidDateRange(date);
        }

        return date;
    }
}