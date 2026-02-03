using System.Globalization;
using System.Text.RegularExpressions;
using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.AddTransaction.Parsing;

internal sealed partial class AddTransactionCommandRegexParser : IAddTransactionCommandParser
{
    private const string UsageHint = "<type> <category?> <amounts> <date?> <note?>";

    private static readonly string[] StaticExamples =
    [
        "+ food 150",
        "income salary 5000 01.02",
        "expense tax 100.50 20.01.2026 rent"
    ];

    [GeneratedRegex(@"^\s*(?<type>income|expense)\s+(?:(?<category>[a-zA-Zа-яА-ЯёЁ_][^\d\s]*)\s+)?(?<amounts>\d+[.,]?\d*(?!\d+[.,]\d+[.,])(?:\s+(?!\d+[.,]\d+[.,])\d+[.,]?\d*)*)(?:\s+(?<date>\d{1,2}\.\d{1,2}\.\d{2,4}))?\s*(?<notes>.*)?$", RegexOptions.IgnoreCase)]
    private static partial Regex CommandRegex();

    public async Task<AddTransactionCommandParseResult> Parse(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return CreateFailure("The input string is empty.");
        }

        var match = CommandRegex().Match(payload.Trim());
        if (!match.Success)
        {
            return CreateFailure("Invalid syntax.");
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

    private static AddTransactionCommandParseResult.Failure CreateFailure(string reason)
        => new(reason, UsageHint, StaticExamples);

    private static (decimal[]? Values, AddTransactionCommandParseResult.Failure? Error) ExtractAmounts(string value)
    {
        var tokens = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var result = new decimal[tokens.Length];

        for (int i = 0; i < tokens.Length; i++)
        {
            var clean = tokens[i].Replace(',', '.');
            if (!decimal.TryParse(clean, NumberStyles.Number, CultureInfo.InvariantCulture, out var val))
            {
                return (null, CreateFailure($"'{tokens[i]}' is not a valid number."));
            }
            result[i] = val;
        }

        return (result, null);
    }

    private static (DateOnly? Date, AddTransactionCommandParseResult.Failure? Error) ExtractDate(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return (null, null);
        }

        string[] formats = ["dd.MM.yyyy", "dd.MM.yy", "d.M.yyyy", "d.M.yy"];
        if (!DateOnly.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            var supported = string.Join(", ", formats);

            return (null, CreateFailure($"Invalid date: '{value}'. Expected: {supported}"));
        }

        return (date, null);
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

    private static string? ExtractCategory(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
    private static string? ExtractNotes(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}