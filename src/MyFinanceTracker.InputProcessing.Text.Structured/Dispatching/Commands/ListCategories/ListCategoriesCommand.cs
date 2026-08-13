namespace MyFinanceTracker.InputProcessing.Text.Structured.Dispatching.Commands.ListCategories;

[CommandMetadata(
    description: "Listing categories",
    examples: [
        "c all", 
        "cat list"
    ]
)]
public record ListCategoriesCommand() : ITextCommand
{
}