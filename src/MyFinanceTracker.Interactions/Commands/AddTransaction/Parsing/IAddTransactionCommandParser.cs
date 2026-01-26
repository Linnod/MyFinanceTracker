namespace MyFinanceTracker.Interactions.Commands.AddTransaction.Parsing;

internal interface IAddTransactionCommandParser
{
    AddTransactionCommandParseResult Parse(string payload);
}
