using Google.GenAI.Types;
using MediatR;
using MyFinanceTracker.InputProcessing.Text.Gemini.Parsing;
using MyFinanceTracker.InputProcessing.Text.Gemini.Declarations;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.UseCases.Category.List;
using MyFinanceTracker.UseCases.Transaction.Create;
using MyFinanceTracker.UseCases.Transaction.Delete;
using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.InputProcessing.Text.Gemini.Execution;

internal sealed partial class GeminiToolExecutor(
    IMediator mediator, 
    ILogger<GeminiToolExecutor> logger) : IGeminiToolExecutor
{
    public async Task<ActionResult> ExecuteToolCall(FunctionCall functionCall, CancellationToken ct)
    {
        LogExecutingTool(functionCall.Name);

        var args = functionCall.Args;

        var result = functionCall.Name switch
        {
            GeminiToolDeclarationProvider.ToolNames.AddTransaction => await HandleAddTransactionAsync(args, ct),
            GeminiToolDeclarationProvider.ToolNames.DeleteTransactions => await HandleDeleteTransactionsAsync(args, ct),
            GeminiToolDeclarationProvider.ToolNames.ListCategories => await HandleListCategoriesAsync(ct),
            _ => new ActionResult.Failure($"Unsupported function call: '{functionCall.Name}'")
        };

        LogExecutedTool(functionCall.Name);
        return result;
    }

    private async Task<ActionResult> HandleAddTransactionAsync(IDictionary<string, object>? args, CancellationToken ct)
    {
        var typeStr = args.GetString("type") ?? "expense";
        var type = string.Equals(typeStr, "income", StringComparison.OrdinalIgnoreCase)
            ? TransactionType.Income
            : TransactionType.Expense;

        var amounts = args.GetDecimalArray("amounts");
        var categoryAlias = args.GetString("categoryAlias");
        var date = args.GetDateOnly("date");
        var note = args.GetString("note");

        var items = amounts.Select(amount => new CreateTransactionItem(
            TransactionType: type,
            Amount: amount,
            CategoryAlias: categoryAlias,
            Date: date,
            Note: note
        )).ToList();

        var request = new CreateTransactionsRequest(items);
        var response = await mediator.Send(request, ct);

        return response switch
        {
            CreateTransactionsResponse.Success s => new ActionResult.Transaction.Added(s.Transactions),

            CreateTransactionsResponse.ValidationError v => new ActionResult.InvalidInput(v.Errors),

            _ => new ActionResult.Failure()
        };
    }

    private async Task<ActionResult> HandleDeleteTransactionsAsync(IDictionary<string, object>? args, CancellationToken ct)
    {
        var categoryAlias = args.GetString("categoryAlias");
        var date = args.GetDateOnly("date");

        var request = new DeleteTransactionsRequest(categoryAlias, date);
        var response = await mediator.Send(request, ct);

        return response switch
        {
            DeleteTransactionsResponse.Success s => new ActionResult.Transaction.Deleted(s.CategoryName, s.Date),

            DeleteTransactionsResponse.ValidationError v => new ActionResult.InvalidInput(v.Errors),

            _ => new ActionResult.Failure()
        };
    }

    private async Task<ActionResult> HandleListCategoriesAsync(CancellationToken ct)
    {
        var response = await mediator.Send(new ListCategoriesRequest(), ct);
        
        return response switch
        {
            ListCategoriesResponse.Success success => new ActionResult.Category.Listed(success.Categories),

            _ => new ActionResult.Failure()
        };
    }
}