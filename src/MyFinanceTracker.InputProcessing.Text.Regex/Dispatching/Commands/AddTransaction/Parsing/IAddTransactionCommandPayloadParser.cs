namespace MyFinanceTracker.InputProcessing.Text.Regex.Dispatching.Commands.AddTransaction.Parsing;

internal interface IAddTransactionCommandPayloadParser
{
    Task<AddTransactionCommandParseResult> Parse(string payload);
}
