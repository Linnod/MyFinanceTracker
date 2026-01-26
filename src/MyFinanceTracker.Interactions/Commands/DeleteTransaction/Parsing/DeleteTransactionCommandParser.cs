using System.Text.RegularExpressions;

namespace MyFinanceTracker.Interactions.Commands.DeleteTransaction.Parsing;

internal sealed partial class DeleteTransactionCommandParser : IDeleteTransactionCommandParser
{
    [GeneratedRegex(@"^\s*(?<category>[a-zA-Zа-яА-ЯёЁ_][^\d\s]*)\s+(?<date>\S+)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex CommandRegex();

    public DeleteTransactionCommandParseResult Parse(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return new DeleteTransactionCommandParseResult.EmptyInput();
        }

        var match = CommandRegex().Match(payload.Trim());
        if (!match.Success)
        {
            return new DeleteTransactionCommandParseResult.InvalidFormat();
        }

        var dateValue = match.Groups["date"].Value;
        var (date, dateError) = ExtractDate(dateValue);
        if (dateError != null)
        {
            return dateError;
        }

        var commandData = new RawDeleteTransactionCommand(
            CategoryAlias: match.Groups["category"].Value,
            Date: date!.Value
        );

        return new DeleteTransactionCommandParseResult.Success(commandData);
    }

    private static (DateOnly? Date, DeleteTransactionCommandParseResult? Error) ExtractDate(string value)
    {
        string[] formats = ["dd.MM.yyyy", "dd.MM.yy", "d.M.yyyy", "d.M.yy"];
        if (!DateOnly.TryParseExact(value, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var date))
        {
            return (null, new DeleteTransactionCommandParseResult.UnparseableDate(value));
        }

        return (date, null);
    }
}