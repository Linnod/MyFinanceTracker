namespace MyFinanceTracker.InputProcessing.Text.Structured.Dispatching.Commands.DeleteTransaction;

[CommandMetadata(
    description: "Cleaning a category",
    examples: [
        "t rem food 03.02.2026",
        "t rem taxi 04.02.2026"
    ]
)]
public record DeleteTransactionCommand(string Payload) : ITextCommand
{
    public override string ToString() => $"{Payload}";
}