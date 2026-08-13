namespace MyFinanceTracker.InputProcessing.Text.Structured.Dispatching.Commands.DeleteTransaction.Parsing;

internal interface IDeleteTransactionCommandPayloadParser
{
    Task<DeleteTransactionCommandParseResult> Parse(string payload);  
}
