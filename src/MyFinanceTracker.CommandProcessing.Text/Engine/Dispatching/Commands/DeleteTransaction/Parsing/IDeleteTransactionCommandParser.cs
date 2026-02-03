namespace MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.DeleteTransaction.Parsing;

internal interface IDeleteTransactionCommandParser
{
    Task<DeleteTransactionCommandParseResult> Parse(string payload);  
}
