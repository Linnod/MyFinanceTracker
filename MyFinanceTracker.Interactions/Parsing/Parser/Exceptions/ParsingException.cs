using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.Interactions.Parsing.Parser.Exceptions;

internal class ParsingException : Exception
{
    private ParsingException(string message) : base(message) { }

    public static ParsingException EmptyInput() => new("Input string cannot be empty or whitespace.");

    public static ParsingException InvalidAmount(string value)
        => new($"'{value}' is not a valid number.");

    public static ParsingException InvalidDate(string value)
        => new($"'{value}' is not a valid date. Use dd.MM.yyyy, dd.MM.yy, d.M.yyyy, d.M.yy.");

    public static ParsingException InvalidDateRange(DateOnly date)
        => new($"Date {date:dd.MM.yyyy} is out of range ({FinancialRules.MinAllowedYear}-{FinancialRules.MaxAllowedYear}).");
}