using System.Diagnostics;
using Google.GenAI.Types;
using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.InputProcessing.Gemini.Declarations;
using MyFinanceTracker.InputProcessing.Gemini.Parsing;
using MyFinanceTracker.UseCases;
using MyFinanceTracker.UseCases.Category.List;
using MyFinanceTracker.UseCases.Transaction.Create;
using MyFinanceTracker.UseCases.Transaction.Delete;
using MyFinanceTracker.UseCases.Transaction.Get;
using MyFinanceTracker.UseCases.Transaction.Update;

namespace MyFinanceTracker.InputProcessing.Gemini.Execution;

internal sealed partial class GeminiToolExecutor(
    ISender sender,
    TimeProvider timeProvider,
    ILogger<GeminiToolExecutor> logger) : IGeminiToolExecutor
{
    private const string DefaultRawInputForAddingTransaction = "Adding transaction";
    private const string DefaultRawInputForDeletingTransactions = "Deleting transactions";
    private const string DefaultRawInputForUpdatingTransaction = "Updating transaction";
    private const string DefaultRawInputForListingCategories = "Listing Categories";
    private const string DefaultRawInputForGettingTransactions = "Getting transactions";

    public async Task<ActionResult> ExecuteToolCall(FunctionCall functionCall, CancellationToken ct)
    {
        LogExecutingTool(functionCall.Name);

        var args = functionCall.Args;

        var result = functionCall.Name switch
        {
            GeminiToolDeclarationProvider.ToolNames.AddTransaction => await HandleAddTransaction(args, ct),
            GeminiToolDeclarationProvider.ToolNames.DeleteTransactions => await HandleDeleteTransactions(args, ct),
            GeminiToolDeclarationProvider.ToolNames.UpdateTransaction => await HandleUpdateTransaction(args, ct),
            GeminiToolDeclarationProvider.ToolNames.ListCategories => await HandleListCategories(args, ct),
            GeminiToolDeclarationProvider.ToolNames.GetTransactions => await HandleGetTransactions(args, ct),
            _ => throw new UnreachableException($"Unsupported function call: '{functionCall.Name}'")
        };

        LogExecutedTool(functionCall.Name);
        return result;
    }

    private async Task<ActionResult> HandleAddTransaction(IDictionary<string, object>? args, CancellationToken ct)
    {
        var typedArgs = args.BindArgs<AddTransactionArgs>();
        if (typedArgs is null)
        {
            return new ActionResult(DefaultRawInputForAddingTransaction, new CreateTransactionsResponse.Failure());
        }

        var rawInput = GetRecognizedInput(typedArgs.RecognizedInput, DefaultRawInputForAddingTransaction);
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var date = typedArgs.Date ?? today;

        var items = typedArgs.Amounts.Select(amount => new CreateTransactionItem(
            TransactionType: typedArgs.Type,
            Amount: amount,
            CategoryAlias: typedArgs.CategoryAlias ?? string.Empty,
            Date: date,
            Note: typedArgs.Note
        )).ToList();

        var request = new CreateTransactionsRequest(items);
        var response = await sender.Send(request, ct);

        return new ActionResult(rawInput, response);
    }

    private async Task<ActionResult> HandleDeleteTransactions(IDictionary<string, object>? args, CancellationToken ct)
    {
        var typedArgs = args.BindArgs<DeleteTransactionsArgs>();
        if (typedArgs is null)
        {
            return new ActionResult(DefaultRawInputForDeletingTransactions, new DeleteTransactionsResponse.Failure());
        }

        var rawInput = GetRecognizedInput(typedArgs.RecognizedInput, DefaultRawInputForDeletingTransactions);

        var request = new DeleteTransactionsRequest(typedArgs.Ids);
        var response = await sender.Send(request, ct);

        return new ActionResult(rawInput, response);
    }

    private async Task<ActionResult> HandleUpdateTransaction(IDictionary<string, object>? args, CancellationToken ct)
    {
        var typedArgs = args.BindArgs<UpdateTransactionArgs>();
        if (typedArgs is null)
        {
            return new ActionResult(DefaultRawInputForUpdatingTransaction, new UpdateTransactionResponse.Failure());
        }

        var rawInput = GetRecognizedInput(typedArgs.RecognizedInput, DefaultRawInputForUpdatingTransaction);

        var request = new UpdateTransactionRequest(
            Id: typedArgs.Id ?? Guid.Empty,
            Type: typedArgs.Type,
            Amount: typedArgs.Amount,
            CategoryAlias: typedArgs.CategoryAlias,
            Date: typedArgs.Date,
            Note: typedArgs.Note,
            ClearNote: typedArgs.ClearNote ?? false);

        var response = await sender.Send(request, ct);

        return new ActionResult(rawInput, response);
    }

    private async Task<ActionResult> HandleGetTransactions(
        IDictionary<string, object>? args,
        CancellationToken ct)
    {
        var typedArgs = args.BindArgs<GetTransactionsArgs>();
        if (typedArgs is null)
        {
            return new ActionResult(DefaultRawInputForGettingTransactions, new GetTransactionsResponse.Failure());
        }

        var rawInput = GetRecognizedInput(
            typedArgs.RecognizedInput,
            DefaultRawInputForGettingTransactions);

        if (typedArgs.FromDate is null || typedArgs.ToDate is null)
        {
            var errors = new List<ValidationErrorItem>();
            if (typedArgs.FromDate is null)
            {
                errors.Add(new ValidationErrorItem(ValidationErrorCode.Transaction.FromDateRequired));
            }

            if (typedArgs.ToDate is null)
            {
                errors.Add(new ValidationErrorItem(ValidationErrorCode.Transaction.ToDateRequired));
            }

            return new ActionResult(rawInput, new GetTransactionsResponse.ValidationError(errors));
        }

        var request = new GetTransactionsRequest(
            typedArgs.FromDate.Value,
            typedArgs.ToDate.Value,
            typedArgs.CategoryAliases,
            typedArgs.Type);

        var response = await sender.Send(request, ct);

        return new ActionResult(rawInput, response);
    }

    private async Task<ActionResult> HandleListCategories(IDictionary<string, object>? args, CancellationToken ct)
    {
        var typedArgs = args.BindArgs<ListCategoriesArgs>();
        if (typedArgs is null)
        {
            return new ActionResult(DefaultRawInputForListingCategories, new ListCategoriesResponse.Failure());
        }

        var rawInput = GetRecognizedInput(typedArgs.RecognizedInput, DefaultRawInputForListingCategories);

        var response = await sender.Send(new ListCategoriesRequest(), ct);

        return new ActionResult(rawInput, response);
    }

    private static string GetRecognizedInput(string? recognizedInput, string fallback) =>
        string.IsNullOrWhiteSpace(recognizedInput) ? fallback : recognizedInput;
}
