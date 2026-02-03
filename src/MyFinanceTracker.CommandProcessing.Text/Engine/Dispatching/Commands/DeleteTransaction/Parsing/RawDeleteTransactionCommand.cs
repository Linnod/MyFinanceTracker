namespace MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.DeleteTransaction.Parsing;

internal record RawDeleteTransactionCommand(
    string CategoryAlias,
    DateOnly Date
)
{
    public override string ToString() => 
        $"Delete from '{CategoryAlias}' for {Date:dd.MM.yyyy}";
}