using System.Globalization;
using System.Text.RegularExpressions;

namespace MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.DeleteTransaction.Parsing;

internal sealed partial class DeleteTransactionCommandPayloadRegexParser : IDeleteTransactionCommandPayloadParser
{
    [GeneratedRegex(@"^\s*(?<category>[a-zA-Zа-яА-ЯёЁ_][^\d\s]*)\s+(?<date>\S+)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex CommandRegex();

    public async Task<DeleteTransactionCommandParseResult> Parse(string payload)
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

        var dateValue = match.Groups["date"].Value;
        var (date, dateError) = ExtractDate(dateValue);
        if (dateError != null)
        {
            return dateError;
        }

        var commandData = new DeleteTransactionParsedPayload(
            CategoryAlias: match.Groups["category"].Value,
            Date: date!.Value
        );

        return new DeleteTransactionCommandParseResult.Success(commandData);
    }

    private static DeleteTransactionCommandParseResult.Failure CreateFailure(string reason)
        => new(reason);

    private static (DateOnly? Date, DeleteTransactionCommandParseResult.Failure? Error) ExtractDate(string value)
    {
        string[] formats = ["dd.MM.yyyy", "dd.MM.yy", "d.M.yyyy", "d.M.yy"];
        if (!DateOnly.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            var supported = string.Join(", ", formats);

            return (null, CreateFailure($"Invalid date: '{value}'. Expected: {supported}"));
        }

        return (date, null);
    }
}