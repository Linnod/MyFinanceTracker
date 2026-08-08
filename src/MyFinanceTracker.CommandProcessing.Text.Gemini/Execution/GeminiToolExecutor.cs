using Google.GenAI.Types;
using MediatR;
using MyFinanceTracker.CommandProcessing.Text.Gemini.Parsing;
using MyFinanceTracker.CommandProcessing.Text.Gemini.Declarations;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.UseCases.Category.List;
using MyFinanceTracker.UseCases.Transaction.Create;
using MyFinanceTracker.UseCases.Transaction.Delete;
using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.CommandProcessing.Text.Gemini.Execution;

internal sealed partial class GeminiToolExecutor(
    IMediator mediator, 
    ILogger<GeminiToolExecutor> logger) : IGeminiToolExecutor
{
    public async Task<TextCommandResponse> ExecuteToolCallAsync(FunctionCall functionCall, CancellationToken ct = default)
    {
        LogExecutingTool(functionCall.Name);

        var args = functionCall.Args;

        var result = functionCall.Name switch
        {
            GeminiToolDeclarationProvider.ToolNames.AddTransaction => await HandleAddTransactionAsync(args, ct),
            GeminiToolDeclarationProvider.ToolNames.DeleteTransactions => await HandleDeleteTransactionsAsync(args, ct),
            GeminiToolDeclarationProvider.ToolNames.ListCategories => await HandleListCategoriesAsync(ct),
            _ => new TextCommandResponse.InvalidInput($"Unsupported function call: '{functionCall.Name}'")
        };

        LogExecutedTool(functionCall.Name);
        return result;
    }

    private async Task<TextCommandResponse> HandleAddTransactionAsync(IDictionary<string, object>? args, CancellationToken ct)
    {
        var typeStr = args.GetString("type") ?? "expense";
        var type = string.Equals(typeStr, "income", StringComparison.OrdinalIgnoreCase)
            ? TransactionType.Income
            : TransactionType.Expense;

        var amounts = args.GetDecimalArray("amounts");
        var categoryAlias = args.GetString("categoryAlias");
        var date = args.GetDateOnly("date");
        var note = args.GetString("note");

        var request = new CreateTransactionRequest(type, amounts, categoryAlias, date, note);
        var result = await mediator.Send(request, ct);

        return MapCreateResponse(result);
    }

    private async Task<TextCommandResponse> HandleDeleteTransactionsAsync(IDictionary<string, object>? args, CancellationToken ct)
    {
        var categoryAlias = args.GetString("categoryAlias");
        var date = args.GetDateOnly("date");

        var request = new DeleteTransactionsRequest(categoryAlias, date);
        var result = await mediator.Send(request, ct);

        return MapDeleteResponse(result);
    }

    private async Task<TextCommandResponse> HandleListCategoriesAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new ListCategoriesRequest(), ct);
        return result switch
        {
            ListCategoriesResponse.Success success => new TextCommandResponse.Success(
                CommandDescription: "Listing categories",
                PrimaryValue: "Available categories and aliases:",
                Details: [.. success.Categories.Select(c => new TextCommandResponseDetail(
                    Name: $"{c.Name} ({c.Id})",
                    Value: string.Join(", ", c.Aliases),
                    Icon: c.IsIncome ? "💰" : "💸"
                ))]),

            _ => new TextCommandResponse.LogicError("Failed to retrieve categories.")
        };
    }

    private static TextCommandResponse MapCreateResponse(CreateTransactionResponse response) => response switch
    {
        CreateTransactionResponse.Success s => new TextCommandResponse.Success(
            CommandDescription: "Adding transaction via Gemini AI",
            PrimaryValue: $"{s.Amounts.Sum()} added to category '{s.CategoryName}'",
            Details: BuildCreateDetails(s)),

        CreateTransactionResponse.ValidationError v => new TextCommandResponse.InvalidInput(
            Details: string.Join("\n", v.Errors.Select(e => $"• {e.Message}")),
            Suggestion: v.Errors.FirstOrDefault(e => e.Suggestion != null)?.Suggestion),

        _ => new TextCommandResponse.SystemError("Domain service failure.")
    };

    private static List<TextCommandResponseDetail> BuildCreateDetails(CreateTransactionResponse.Success s)
    {
        var details = new List<TextCommandResponseDetail>
        {
            new("Date", s.Date.ToString("dd.MM.yyyy"), "📅")
        };

        if (s.Amounts.Count > 1)
        {
            details.Add(new("Breakdown", string.Join(" + ", s.Amounts), "🔢"));
        }

        if (!string.IsNullOrWhiteSpace(s.Note))
        {
            details.Add(new("Note", s.Note, "📝"));
        }

        return details;
    }

    private static TextCommandResponse MapDeleteResponse(DeleteTransactionsResponse response) => response switch
    {
        DeleteTransactionsResponse.Success s => new TextCommandResponse.Success(
            CommandDescription: "Clearing category via Gemini AI",
            PrimaryValue: $"Cleared category '{s.CategoryName}'",
            Details: [new("Date", s.Date.ToString("dd.MM.yyyy"), "📅")]),

        DeleteTransactionsResponse.ValidationError v => new TextCommandResponse.InvalidInput(
            Details: string.Join("\n", v.Errors.Select(e => $"• {e.Message}")),
            Suggestion: v.Errors.FirstOrDefault(e => e.Suggestion != null)?.Suggestion),

        _ => new TextCommandResponse.SystemError("Domain service failure.")
    };
}