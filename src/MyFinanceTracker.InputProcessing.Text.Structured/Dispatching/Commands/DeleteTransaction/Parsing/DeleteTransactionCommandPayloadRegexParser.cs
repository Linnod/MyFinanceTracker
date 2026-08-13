using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.InputProcessing.Text.Structured.Dispatching.Commands.DeleteTransaction.Parsing;

internal sealed partial class DeleteTransactionCommandPayloadRegexParser(
    ILogger<DeleteTransactionCommandPayloadRegexParser> logger) : IDeleteTransactionCommandPayloadParser
{
    [GeneratedRegex(@"^\s*(?<category>[a-zA-Zа-яА-ЯёЁ_][^\d\s]*)\s+(?<date>\S+)\s*$", RegexOptions.IgnoreCase)]
    private static partial System.Text.RegularExpressions.Regex CommandRegex();

    public Task<DeleteTransactionCommandParseResult> Parse(string payload)
    {
        var result = ParseInternal(payload);

        if (result is DeleteTransactionCommandParseResult.Success success)
        {
            LogParseSuccess(success);
        }
        else if (result is DeleteTransactionCommandParseResult.Failure failure)
        {
            LogParseFailure(failure);
        }

        return Task.FromResult(result);
    }

    private static DeleteTransactionCommandParseResult ParseInternal(string payload)
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
            return (null, CreateFailure(ErrorCode.Syntax.InvalidDateFormat));
        }

        return (date, null);
    }
}