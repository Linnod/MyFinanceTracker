namespace MyFinanceTracker.CommandProcessing.Text.Regex.Dispatching.Commands.DeleteTransaction;

[CommandMetadata(
    description: "Cleaning a category",
    usageHint: "rem <category> <date>",
    examples: ["rem food 03.02.2026", "rem taxi 04.02.2026"]
)]
public record DeleteTransactionCommand(string Payload) : ITextCommand
{
    public override string ToString() => $"{Payload}";
}