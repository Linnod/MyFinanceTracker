namespace MyFinanceTracker.CommandProcessing.Text.Regex.Dispatching.Commands.ListCategories;

[CommandMetadata(
    description: "Listing categories"
)]
public record ListCategoriesCommand() : ITextCommand
{
}