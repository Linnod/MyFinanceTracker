using System.Globalization;
using System.Text.RegularExpressions;
using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.Interactions.Commands.AddTransaction.Parsing;

internal sealed partial class AddTransactionCommandParser : IAddTransactionCommandParser
{
    [GeneratedRegex(@"^\s*(?<type>income|expense)\s+(?:(?<category>[a-zA-Zа-яА-ЯёЁ_][^\d\s]*)\s+)?(?<amounts>\d+[.,]?\d*(?!\d+[.,]\d+[.,])(?:\s+(?!\d+[.,]\d+[.,])\d+[.,]?\d*)*)(?:\s+(?<date>\d{1,2}\.\d{1,2}\.\d{2,4}))?\s*(?<notes>.*)?$", RegexOptions.IgnoreCase)]
    private static partial Regex CommandRegex();

    public AddTransactionCommandParseResult Parse(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return new AddTransactionCommandParseResult.EmptyInput();
        }

        var match = CommandRegex().Match(payload.Trim());
        if (!match.Success)
        {
            return new AddTransactionCommandParseResult.InvalidFormat();
        }

        var (amounts, amountsError) = ExtractAmounts(match.Groups["amounts"].Value);
        if (amountsError != null)
        {
            return amountsError;
        }

        var (date, dateError) = ExtractDate(match.Groups["date"].Value);
        if (dateError != null)
        {
            return dateError;
        }

        return new AddTransactionCommandParseResult.Success(new RawAddTransactionCommand(
            Type: ExtractType(match.Groups["type"].Value),
            CategoryAlias: ExtractCategory(match.Groups["category"].Value),
            Amounts: amounts!,
            Date: date,
            Note: ExtractNotes(match.Groups["notes"].Value)
        ));
    }

    private static TransactionType ExtractType(string value)
    {
        return value.ToLower() switch
        {
            FinancialRules.DefaultIncomeCategoryAlias => TransactionType.Income,
            "expense" => TransactionType.Expense,
            _ => throw new InvalidOperationException($"Critical: Regex allowed type '{value}', but ExtractType is not synced.")
        };
    }

    private static (decimal[]? Values, AddTransactionCommandParseResult? Error) ExtractAmounts(string value)
    {
        var tokens = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var result = new decimal[tokens.Length];

        for (int i = 0; i < tokens.Length; i++)
        {
            var clean = tokens[i].Replace(',', '.');
            if (!decimal.TryParse(clean, NumberStyles.Number, CultureInfo.InvariantCulture, out var val))
            {
                return (null, new AddTransactionCommandParseResult.InvalidAmount(tokens[i]));
            }
            result[i] = val;
        }

        return (result, null);
    }

    private static (DateOnly? Date, AddTransactionCommandParseResult? Error) ExtractDate(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return (null, null);
        }

        string[] formats = ["dd.MM.yyyy", "dd.MM.yy", "d.M.yyyy", "d.M.yy"];
        if (!DateOnly.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return (null, new AddTransactionCommandParseResult.UnparseableDate(value));
        }

        return (date, null);
    }

    private static string? ExtractCategory(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static string? ExtractNotes(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}