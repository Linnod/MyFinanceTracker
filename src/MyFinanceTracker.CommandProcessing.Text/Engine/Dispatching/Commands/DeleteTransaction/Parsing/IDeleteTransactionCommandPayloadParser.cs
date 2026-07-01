namespace MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.DeleteTransaction.Parsing;

internal interface IDeleteTransactionCommandPayloadParser
{
    Task<DeleteTransactionCommandParseResult> Parse(string payload);  
}
