namespace MyFinanceTracker.InputProcessing.Text.Regex.Dispatching.Commands.AddTransaction;

[CommandMetadata(
    description: "Adding a transaction",
    examples: [
        "t add expense food 150", 
        "t add income salary 5000 01.02", 
        "t add expense tax 100.50 20.01.2026 rent"
    ]
)]
public record AddTransactionCommand(string Payload) : ITextCommand
{
    public override string ToString() => $"{Payload}";
}