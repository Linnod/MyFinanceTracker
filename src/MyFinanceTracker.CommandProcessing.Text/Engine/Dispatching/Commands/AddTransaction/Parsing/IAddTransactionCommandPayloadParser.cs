namespace MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.AddTransaction.Parsing;

internal interface IAddTransactionCommandPayloadParser
{
    Task<AddTransactionCommandParseResult> Parse(string payload);
}
