namespace MyFinanceTracker.InputProcessing.Text.Structured.Dispatching.Commands.AddTransaction.Parsing;

internal interface IAddTransactionCommandPayloadParser
{
    Task<AddTransactionCommandParseResult> Parse(string payload);
}
