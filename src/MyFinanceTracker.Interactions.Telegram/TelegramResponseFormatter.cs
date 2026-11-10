using System.Diagnostics;
using System.Net;
using System.Text;
using MyFinanceTracker.Common.Notifications;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.InputProcessing;
using MyFinanceTracker.UseCases;
using MyFinanceTracker.UseCases.Category.List;
using MyFinanceTracker.UseCases.Transaction.Create;
using MyFinanceTracker.UseCases.Transaction.Delete;
using MyFinanceTracker.UseCases.Transaction.Get;
using MyFinanceTracker.UseCases.Transaction.Update;

namespace MyFinanceTracker.Interactions.Telegram;

internal static class TelegramResponseFormatter
{
    private const int MaxMessageLength = 3800;
    private const string OverflowMessageTemplate = "<i>...and {0} more transaction(s) hidden.</i>";

    public static string FormatNotification(OutboxSyncNotification notification) => notification switch
    {
        OutboxSyncNotification.MessageMovedToDeadLetter deadLetter => $"""
            <b>Outbox Message Moved to Dead Letter</b>
            ID: {deadLetter.OriginalOutboxMessageId}
            Operation: {WebUtility.HtmlEncode(deadLetter.OperationType)}
            Error: {WebUtility.HtmlEncode(deadLetter.ErrorMessage)}
            """,

        OutboxSyncNotification.SyncFailed syncFailed => $"""
            <b>Outbox Sync Failed</b>
            {WebUtility.HtmlEncode(syncFailed.ErrorMessage)}
            """,

        _ => throw new UnreachableException($"Unknown notification type: {notification.GetType()}")
    };

    public static string FormatResponse(ProcessingResult response) => response switch
    {
        ProcessingResult.Completed completed => FormatCompleted(completed),

        ProcessingResult.InvalidInput invalid => $"""
            <b>Input Error</b>
            {WebUtility.HtmlEncode(invalid.Details)}
            """,

        ProcessingResult.SystemError systemError => $"""
            <b>System Hiccup</b>
            Something went wrong. We're looking into it.
            <i>{WebUtility.HtmlEncode(systemError.Message)}</i>
            """,

        _ => throw new UnreachableException($"Unknown response type: {response.GetType()}")
    };

    private static string FormatCompleted(ProcessingResult.Completed completed)
    {
        var sb = new StringBuilder();

        foreach (var action in completed.Actions)
        {
            AppendUseCaseResponse(sb, action);
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private static void AppendUseCaseResponse(
        StringBuilder sb,
        UseCaseResponse response)
    {
        switch (response)
        {
            case CreateTransactionsResponse.Success created:
                AppendCreated(sb, created);
                break;

            case UpdateTransactionResponse.Success updated:
                AppendUpdated(sb, updated);
                break;

            case DeleteTransactionsResponse.Success deleted:
                AppendDeleted(sb, deleted);
                break;

            case GetTransactionsResponse.Success listed:
                AppendListed(sb, listed);
                break;

            case ListCategoriesResponse.Success categories:
                AppendCategories(sb, categories);
                break;

            case IValidationErrorResponse validationError:
                AppendValidationErrors(sb, validationError);
                break;

            case IFailureResponse:
                AppendFailure(sb);
                break;

            default:
                throw new UnreachableException($"Unknown use case response type: {response.GetType()}");
        }
    }

    private static void AppendCreated(StringBuilder sb, CreateTransactionsResponse.Success created)
    {
        sb.AppendLine($"<b>Created {created.Transactions.Count} transaction(s)</b>");
        sb.AppendLine();
        AppendTransactions(sb, created.Transactions);
    }

    private static void AppendUpdated(StringBuilder sb, UpdateTransactionResponse.Success updated)
    {
        sb.AppendLine("<b>Updated transaction</b>");
        sb.AppendLine();
        AppendTransaction(sb, updated.Transaction);
    }

    private static void AppendDeleted(StringBuilder sb, DeleteTransactionsResponse.Success deleted) =>
        sb.AppendLine($"Deleted <b>{deleted.DeletedCount}</b> transaction(s)");

    private static void AppendListed(StringBuilder sb, GetTransactionsResponse.Success listed)
    {
        var datePart = FormatDateRange(listed.FromDate, listed.ToDate);
        var header = listed.CategoryNames.Count > 0
            ? $"<b>Transactions in '{WebUtility.HtmlEncode(string.Join(", ", listed.CategoryNames))}'</b> {datePart}"
            : $"<b>Transactions</b> {datePart}";

        sb.AppendLine(header);
        sb.AppendLine();

        if (listed.Transactions.Items.Count == 0)
        {
            sb.AppendLine("No transactions found.");
            return;
        }

        AppendTransactions(sb, listed.Transactions.Items);
    }

    private static void AppendCategories(StringBuilder sb, ListCategoriesResponse.Success categories)
    {
        foreach (var c in categories.Categories)
        {
            sb.AppendLine($"• <code>{WebUtility.HtmlEncode(c.Name)}</code>");
        }
    }

    private static void AppendValidationErrors(StringBuilder sb, IValidationErrorResponse validationError)
    {
        sb.AppendLine("<b>Validation Error(s):</b>");

        foreach (var err in validationError.Errors)
        {
            sb.AppendLine($"• {WebUtility.HtmlEncode(FormatValidationError(err))}");
        }
    }

    private static void AppendFailure(StringBuilder sb) =>
        sb.AppendLine("<b>Action Failed</b>");

    private static void AppendTransactions(
        StringBuilder sb,
        IReadOnlyList<Transaction> transactions)
    {
        var displayedCount = 0;

        foreach (var t in transactions)
        {
            var itemSb = new StringBuilder();
            if (displayedCount > 0)
            {
                itemSb.AppendLine();
            }

            AppendTransaction(itemSb, t);
            var itemText = itemSb.ToString();

            if (sb.Length + itemText.Length + Environment.NewLine.Length > MaxMessageLength)
            {
                break;
            }

            sb.AppendLine(itemText);
            displayedCount++;
        }

        var remaining = transactions.Count - displayedCount;
        if (remaining > 0)
        {
            sb.AppendLine();
            sb.AppendLine(string.Format(OverflowMessageTemplate, remaining));
        }
    }

    private static void AppendTransaction(StringBuilder sb, Transaction t)
    {
        var sign = t.Type == TransactionType.Expense ? "-" : "+";
        sb.AppendLine($"<b>{sign}{t.Amount:0.00}");
        sb.AppendLine($"{WebUtility.HtmlEncode(t.Category.Name)}</b>");

        if (!string.IsNullOrWhiteSpace(t.Note))
        {
            sb.AppendLine(WebUtility.HtmlEncode(t.Note));
        }

        sb.AppendLine($"{t.Date:dd.MM.yyyy HH:mm:ss}");
        sb.Append($"<code>{t.Id}</code>");
    }

    private static string FormatValidationError(ValidationErrorItem err) => err.ErrorCode switch
    {
        ValidationErrorCode.Transaction.CategoryRequired => "Category is required.",
        ValidationErrorCode.Transaction.CategoryNotFound => err.AttemptedValue is not null
            ? $"Category '{err.AttemptedValue}' was not found."
            : "Category was not found.",
        ValidationErrorCode.Transaction.CategoryArchived => err.AttemptedValue is not null
            ? $"Category '{err.AttemptedValue}' is archived and cannot be used for new transactions."
            : "Category is archived and cannot be used for new transactions.",
        ValidationErrorCode.Transaction.IdRequired => "At least one transaction ID is required.",
        ValidationErrorCode.Transaction.InvalidId => err.AttemptedValue is not null
            ? $"Transaction ID '{err.AttemptedValue}' is invalid."
            : "Transaction ID is invalid.",
        ValidationErrorCode.Transaction.TransactionNotFound => err.AttemptedValue is not null
            ? $"Transaction '{err.AttemptedValue}' was not found."
            : "Transaction was not found.",
        ValidationErrorCode.Transaction.FromDateRequired => "Start date is required.",
        ValidationErrorCode.Transaction.ToDateRequired => "End date is required.",
        ValidationErrorCode.Transaction.DateOutOfRange => err.AttemptedValue is not null
            ? $"Year '{err.AttemptedValue}' is out of the allowed range ({FinancialRules.MinAllowedYear}–{FinancialRules.MaxAllowedYear})."
            : $"Date year is out of the allowed range ({FinancialRules.MinAllowedYear}–{FinancialRules.MaxAllowedYear}).",
        ValidationErrorCode.Transaction.InvalidDateRange => "Start date must be on or before end date.",
        ValidationErrorCode.Transaction.AmountRequired => "At least one amount is required.",
        ValidationErrorCode.Transaction.AmountMustBePositive => "Amount must be greater than zero.",
        ValidationErrorCode.Transaction.NothingToUpdate => "At least one field must be provided to update the transaction.",
        _ => err.ErrorCode
    };

    private static string FormatDateRange(DateTime from, DateTime to) =>
        $"from {from:dd.MM.yyyy HH:mm:ss} to {to:dd.MM.yyyy HH:mm:ss}";
}
