using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.InputProcessing.Text.Structured.Dispatching.Commands.AddTransaction.Parsing;

internal sealed partial class AddTransactionCommandPayloadRegexParser(
    ILogger<AddTransactionCommandPayloadRegexParser> logger) : IAddTransactionCommandPayloadParser
{
    [GeneratedRegex(@"^\s*(?<type>income|expense)\s+(?:(?<category>[a-zA-Zа-яА-ЯёЁ_][^\d\s]*)\s+)?(?<amounts>(?<!\S)\d+(?:[.,]\d+)?(?!\S)(?:\s+(?<!\S)\d+(?:[.,]\d+)?(?!\S))*)(?:\s+(?<date>\d{1,2}\.\d{1,2}\.\d{2,4}))?\s*(?<notes>.*)?$", RegexOptions.IgnoreCase)]
    private static partial System.Text.RegularExpressions.Regex CommandRegex();

    public Task<AddTransactionCommandParseResult> Parse(string payload)
    {
        var result = ParseInternal(payload);

        if (result is AddTransactionCommandParseResult.Success success)
        {
            LogParseSuccess(success);
        }
        else if (result is AddTransactionCommandParseResult.Failure failure)
        {
            LogParseFailure(failure);
        }

        return Task.FromResult(result);
    }

    private static AddTransactionCommandParseResult ParseInternal(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return CreateFailure(ErrorCode.Syntax.EmptyInput);
        }

        var match = CommandRegex().Match(payload.Trim());
        if (!match.Success)
        {
            return CreateFailure(ErrorCode.Syntax.InvalidFormat);
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

        return new AddTransactionCommandParseResult.Success(new AddTransactionParsedPayload(
            Type: ExtractType(match.Groups["type"].Value),
            CategoryAlias: ExtractCategory(match.Groups["category"].Value),
            Amounts: amounts!,
            Date: date,
            Note: ExtractNotes(match.Groups["notes"].Value)
        ));
    }

    private static AddTransactionCommandParseResult.Failure CreateFailure(string errorCode)
        => new(errorCode);

    private static (decimal[]? Values, AddTransactionCommandParseResult.Failure? Error) ExtractAmounts(string value)
    {
        var tokens = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var result = new decimal[tokens.Length];

        for (int i = 0; i < tokens.Length; i++)
        {
            var clean = tokens[i].Replace(',', '.');
            if (!decimal.TryParse(clean, NumberStyles.Number, CultureInfo.InvariantCulture, out var val))
            {
                return (null, CreateFailure(ErrorCode.Syntax.InvalidAmount));
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
            return (null, CreateFailure(ErrorCode.Syntax.InvalidDateFormat));
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