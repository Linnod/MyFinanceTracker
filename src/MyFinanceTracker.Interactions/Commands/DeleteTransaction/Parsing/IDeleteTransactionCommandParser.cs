namespace MyFinanceTracker.Interactions.Commands.DeleteTransaction.Parsing;

internal interface IDeleteTransactionCommandParser
{
    DeleteTransactionCommandParseResult Parse(string payload);
}
