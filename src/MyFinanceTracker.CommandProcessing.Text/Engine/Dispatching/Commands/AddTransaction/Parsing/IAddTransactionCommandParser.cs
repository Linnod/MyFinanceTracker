namespace MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.AddTransaction.Parsing;

internal interface IAddTransactionCommandParser
{
    Task<AddTransactionCommandParseResult> Parse(string payload);
}
