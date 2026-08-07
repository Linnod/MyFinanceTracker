namespace MyFinanceTracker.CommandProcessing.Text.Regex.Dispatching.Commands.DeleteTransaction.Parsing;

internal interface IDeleteTransactionCommandPayloadParser
{
    Task<DeleteTransactionCommandParseResult> Parse(string payload);  
}
