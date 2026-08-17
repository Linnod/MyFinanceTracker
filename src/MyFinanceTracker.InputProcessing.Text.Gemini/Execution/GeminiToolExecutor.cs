using System.Diagnostics;
using Google.GenAI.Types;
using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.InputProcessing.Text.Gemini.Declarations;
using MyFinanceTracker.InputProcessing.Text.Gemini.Parsing;
using MyFinanceTracker.UseCases.Category.List;
using MyFinanceTracker.UseCases.Transaction.Create;
using MyFinanceTracker.UseCases.Transaction.Delete;
using MyFinanceTracker.UseCases.Transaction.Get;

namespace MyFinanceTracker.InputProcessing.Text.Gemini.Execution;

internal sealed partial class GeminiToolExecutor(
    IMediator mediator,
    ILogger<GeminiToolExecutor> logger) : IGeminiToolExecutor
{
    private const string DefaultRawInputForAddingTransaction = "Adding transaction";
    private const string DefaultRawInputForDeletingTransactions = "Deleting transactions";
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
            return new ActionResult.Failure() { RawInput = DefaultRawInputForAddingTransaction };
        }

        var rawInput = GetRecognizedInput(typedArgs.RecognizedInput, DefaultRawInputForAddingTransaction);

        var items = typedArgs.Amounts.Select(amount => new CreateTransactionItem(
            TransactionType: typedArgs.Type,
            Amount: amount,
            CategoryAlias: typedArgs.CategoryAlias,
            Date: typedArgs.Date,
            Note: typedArgs.Note
        )).ToList();

        var request = new CreateTransactionsRequest(items);
        var response = await mediator.Send(request, ct);

        return response switch
        {
            CreateTransactionsResponse.Success s => new ActionResult.Transaction.Added(s.Transactions)
            {
                RawInput = rawInput
            },

            CreateTransactionsResponse.ValidationError v => new ActionResult.InvalidInput(v.Errors)
            {
                RawInput = rawInput
            },

            _ => new ActionResult.Failure()
            {
                RawInput = rawInput
            }
        };
    }

    private async Task<ActionResult> HandleDeleteTransactions(IDictionary<string, object>? args, CancellationToken ct)
    {
        var typedArgs = args.BindArgs<DeleteTransactionsArgs>();
        if (typedArgs is null)
        {
            return new ActionResult.Failure() { RawInput = DefaultRawInputForDeletingTransactions };
        }

        var rawInput = GetRecognizedInput(typedArgs.RecognizedInput, DefaultRawInputForDeletingTransactions);

        var request = new DeleteTransactionsRequest(typedArgs.CategoryAlias, typedArgs.Date);
        var response = await mediator.Send(request, ct);

        return response switch
        {
            DeleteTransactionsResponse.Success s => new ActionResult.Transaction.Deleted(s.CategoryName, s.Date)
            {
                RawInput = rawInput
            },

            DeleteTransactionsResponse.ValidationError v => new ActionResult.InvalidInput(v.Errors)
            {
                RawInput = rawInput
            },

            _ => new ActionResult.Failure()
            {
                RawInput = rawInput
            }
        };
    }

    private async Task<ActionResult> HandleGetTransactions(
        IDictionary<string, object>? args,
        CancellationToken ct)
    {
        var typedArgs = args.BindArgs<GetTransactionsArgs>();
        if (typedArgs is null)
        {
            return new ActionResult.Failure
            {
                RawInput = DefaultRawInputForGettingTransactions
            };
        }

        var rawInput = GetRecognizedInput(
            typedArgs.RecognizedInput,
            DefaultRawInputForGettingTransactions);

        var request = new GetTransactionsRequest(
            typedArgs.CategoryAlias,
            typedArgs.Date);

        var response = await mediator.Send(request, ct);

        return response switch
        {
            GetTransactionsResponse.Success s =>
                new ActionResult.Transaction.Listed(
                    s.CategoryName,
                    s.Date,
                    s.Transactions)
                {
                    RawInput = rawInput
                },

            GetTransactionsResponse.ValidationError v =>
                new ActionResult.InvalidInput(v.Errors)
                {
                    RawInput = rawInput
                },

            _ => new ActionResult.Failure
            {
                RawInput = rawInput
            }
        };
    }

    private async Task<ActionResult> HandleListCategories(IDictionary<string, object>? args, CancellationToken ct)
    {
        var typedArgs = args.BindArgs<ListCategoriesArgs>();
        if (typedArgs is null)
        {
            return new ActionResult.Failure() { RawInput = DefaultRawInputForListingCategories };
        }

        var rawInput = GetRecognizedInput(typedArgs.RecognizedInput, DefaultRawInputForListingCategories);

        var response = await mediator.Send(new ListCategoriesRequest(), ct);

        return response switch
        {
            ListCategoriesResponse.Success success => new ActionResult.Category.Listed(success.Categories)
            {
                RawInput = rawInput
            },

            _ => new ActionResult.Failure()
            {
                RawInput = rawInput
            }
        };
    }

    private static string GetRecognizedInput(string? recognizedInput, string fallback) =>
        string.IsNullOrWhiteSpace(recognizedInput) ? fallback : recognizedInput;
}