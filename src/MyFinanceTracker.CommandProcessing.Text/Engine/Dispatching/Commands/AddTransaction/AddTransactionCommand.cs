namespace MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.AddTransaction;

[CommandMetadata(
    description: "Adding a transaction",
    usageHint: "<type> <category?> <amounts> <date?> <note?>",
    examples: ["+ food 150", "income salary 5000 01.02", "expense tax 100.50 20.01.2026 rent"]
)]
public record AddTransactionCommand(string Payload) : ITextCommand
{
    public override string ToString() => $"{Payload}";
}