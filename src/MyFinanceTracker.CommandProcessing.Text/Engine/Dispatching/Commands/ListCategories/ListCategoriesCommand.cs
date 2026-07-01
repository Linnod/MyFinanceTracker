namespace MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.ListCategories;

[CommandMetadata(
    description: "Listing categories"
)]
public record ListCategoriesCommand() : ITextCommand
{
}