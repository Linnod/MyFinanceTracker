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
    public async Task<UseCaseResponse> ExecuteToolCall(FunctionCall functionCall, CancellationToken ct)
    {
        var args = new ToolCallArgs(functionCall.Args);
        LogExecutingTool(functionCall.Name, args);

        var result = functionCall.Name switch
        {
            GeminiToolDeclarationProvider.ToolNames.AddTransaction => await HandleAddTransaction(args, ct),
            GeminiToolDeclarationProvider.ToolNames.DeleteTransactions => await HandleDeleteTransactions(args, ct),
            GeminiToolDeclarationProvider.ToolNames.UpdateTransaction => await HandleUpdateTransaction(args, ct),
            GeminiToolDeclarationProvider.ToolNames.ListCategories => await HandleListCategories(ct),
            GeminiToolDeclarationProvider.ToolNames.GetTransactions => await HandleGetTransactions(args, ct),
            _ => throw new UnreachableException($"Unsupported function call: '{functionCall.Name}'")
        };

        LogExecutedTool(functionCall.Name);
        return result;
    }

    private async Task<UseCaseResponse> HandleAddTransaction(ToolCallArgs args, CancellationToken ct)
    {
        var typedArgs = args.To<AddTransactionArgs>();
        if (typedArgs is null)
        {
            return new CreateTransactionsResponse.Failure();
        }

        var date = typedArgs.Date ?? timeProvider.GetLocalNow().DateTime;

        var items = typedArgs.Amounts.Select(amount => new CreateTransactionItem(
            TransactionType: typedArgs.Type,
            Amount: amount,
            CategoryId: typedArgs.CategoryId ?? string.Empty,
            Date: date,
            Note: typedArgs.Note
        )).ToList();

        var request = new CreateTransactionsRequest(items);
        return await sender.Send(request, ct);
    }

    private async Task<UseCaseResponse> HandleDeleteTransactions(ToolCallArgs args, CancellationToken ct)
    {
        var typedArgs = args.To<DeleteTransactionsArgs>();
        if (typedArgs is null)
        {
            return new DeleteTransactionsResponse.Failure();
        }

        var parsedGuids = new List<Guid>();
        var validationErrors = new List<ValidationErrorItem>();

        foreach (var idStr in typedArgs.Ids)
        {
            if (Guid.TryParse(idStr, out var parsedGuid))
            {
                parsedGuids.Add(parsedGuid);
            }
            else
            {
                validationErrors.Add(new ValidationErrorItem(ValidationErrorCode.Transaction.InvalidId, idStr));
            }
        }

        if (validationErrors.Count > 0)
        {
            return new DeleteTransactionsResponse.ValidationError(validationErrors);
        }

        var request = new DeleteTransactionsRequest(parsedGuids);
        return await sender.Send(request, ct);
    }

    private async Task<UseCaseResponse> HandleUpdateTransaction(ToolCallArgs args, CancellationToken ct)
    {
        var typedArgs = args.To<UpdateTransactionArgs>();
        if (typedArgs is null)
        {
            return new UpdateTransactionResponse.Failure();
        }

        Guid parsedId = Guid.Empty;
        if (!string.IsNullOrWhiteSpace(typedArgs.Id))
        {
            if (!Guid.TryParse(typedArgs.Id, out parsedId))
            {
                return new UpdateTransactionResponse.ValidationError([
                    new ValidationErrorItem(ValidationErrorCode.Transaction.InvalidId, typedArgs.Id)
                ]);
            }
        }

        var request = new UpdateTransactionRequest(
            Id: parsedId,
            Type: typedArgs.Type,
            Amount: typedArgs.Amount,
            CategoryId: typedArgs.CategoryId,
            Date: typedArgs.Date,
            Note: typedArgs.Note,
            ClearNote: typedArgs.ClearNote ?? false);

        return await sender.Send(request, ct);
    }

    private async Task<UseCaseResponse> HandleGetTransactions(
        ToolCallArgs args,
        CancellationToken ct)
    {
        var typedArgs = args.To<GetTransactionsArgs>();
        if (typedArgs is null)
        {
            return new GetTransactionsResponse.Failure();
        }

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

            return new GetTransactionsResponse.ValidationError(errors);
        }

        var request = new GetTransactionsRequest(
            typedArgs.FromDate.Value,
            typedArgs.ToDate.Value,
            typedArgs.CategoryIds,
            typedArgs.Type,
            Take: typedArgs.Limit);

        return await sender.Send(request, ct);
    }

    private async Task<UseCaseResponse> HandleListCategories(CancellationToken ct)
    {
        return await sender.Send(new ListCategoriesRequest(), ct);
    }
}
