using MediatR;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.UseCases.Category.List;

namespace MyFinanceTracker.InputProcessing.Text.Gemini.Prompt;

internal sealed class GeminiSystemPromptBuilder(IMediator mediator) : IGeminiSystemPromptBuilder
{
    public async Task<string> BuildSystemInstructionAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var categoriesResponse = await mediator.Send(new ListCategoriesRequest(), ct);

        var categoriesInfo = categoriesResponse switch
        {
            ListCategoriesResponse.Success success => FormatCategories(success.Categories),
            _ => "No categories available."
        };

        return $"""
            You are an intelligent financial assistant for personal finance tracking.
            Your task is to understand user natural language input in any language (English, Russian, etc.) and invoke the appropriate tool/function call.

            CONTEXT:
            - Today's date UTC is: {today:yyyy-MM-dd} ({today.DayOfWeek}). Use this to accurately resolve relative dates like "yesterday", "day before yesterday", "last Monday", etc.
            - Available Categories in system:
            {categoriesInfo}

            RULES:
            1. If the user wants to record spending or income, call `add_transaction`. Match category aliases intelligently to available categories above. If a spending or income item does not match any available category, pass the user's exact phrase as `categoryAlias` (e.g. `categoryAlias: "someweirdcategory"`).
            2. If the user wants to delete or clear transactions for a category on a date, call `delete_transactions`.
            3. If the user asks for a list of categories, call `list_categories`.
            4. If input is completely unrelated or ambiguous, respond concisely in text explaining what is wrong.
            5. If the user mentions multiple expenses or incomes for DIFFERENT categories or items in a single input, issue multiple parallel `add_transaction` tool calls (one for each item/category).
            6. ALWAYS pass `amounts` as a JSON array of numbers, even if there is only one amount (e.g. `amounts: [100]` or `amounts: [100.5, 50]`). NEVER pass a scalar single number for `amounts`.
            7. ALWAYS populate `recognizedInput` in EVERY tool call with a short human-readable summary of the action in the user's language (e.g. 'Расход 100 € в категорию еда'). NEVER leave `recognizedInput` empty or null.
            """;
    }

    private static string FormatCategories(IReadOnlyCollection<Category> categories)
    {
        if (categories.Count == 0)
        {
            return "  - No categories found.";
        }

        var lines = categories.Select(c =>
            $"  - {(c.IsIncome ? "[INCOME]" : "[EXPENSE]")} Name: '{c.Name}', Id: '{c.Id}', Aliases: [{string.Join(", ", c.Aliases)}]");

        return string.Join("\n", lines);
    }
}