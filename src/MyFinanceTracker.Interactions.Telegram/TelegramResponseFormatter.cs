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
    private const int MaxDisplayedTransactions = 25;
    private const int MaxMessageLength = 3800;

    public static string FormatNotification(OutboxSyncNotification notification) => notification switch
    {
        OutboxSyncNotification.MessageMovedToDeadLetter deadLetter => $"""
            🚨 <b>Outbox Message Moved to Dead Letter</b>
            ID: {deadLetter.OriginalOutboxMessageId}
            Operation: {WebUtility.HtmlEncode(deadLetter.OperationType)}
            Error: {WebUtility.HtmlEncode(deadLetter.ErrorMessage)}
            """,

        OutboxSyncNotification.SyncFailed syncFailed => $"""
            🚨 <b>Outbox Sync Failed</b>
            {WebUtility.HtmlEncode(syncFailed.ErrorMessage)}
            """,

        _ => throw new UnreachableException($"Unknown notification type: {notification.GetType()}")
    };

    public static string FormatResponse(ProcessingResult response) => response switch
    {
        ProcessingResult.Completed completed => FormatCompleted(completed),

        ProcessingResult.InvalidInput invalid => BuildInvalidInputMessage(invalid),

        ProcessingResult.SystemError systemError => $"""
            🔌 <b>System Hiccup</b>
            Something went wrong. We're looking into it.
            <i>Ref: {WebUtility.HtmlEncode(systemError.Message)}</i>
            """,

        _ => throw new UnreachableException($"Unknown response type: {response.GetType()}")
    };

    public static string FormatResponse(UseCaseResponse response, string? rawInput = null)
    {
        var sb = new StringBuilder();
        AppendUseCaseResponse(sb, response, rawInput);
        return sb.ToString().TrimEnd();
    }

    private static string FormatCompleted(ProcessingResult.Completed completed)
    {
        var sb = new StringBuilder();

        foreach (var action in completed.Actions)
        {
            AppendUseCaseResponse(sb, action.Response, action.RawInput);
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private static void AppendUseCaseResponse(
        StringBuilder sb,
        UseCaseResponse response,
        string? rawInput = null)
    {
        switch (response)
        {
            case CreateTransactionsResponse.Success added:
                var totalCreatedCount = added.Transactions.Count;

                sb.AppendLine($"<b>Created {totalCreatedCount} transaction(s)</b>");
                sb.AppendLine();

                AppendTransactions(
                    sb,
                    added.Transactions,
                    "<i>...and {0} more transaction(s) not shown.</i>");
                break;

            case UpdateTransactionResponse.Success updated:
                sb.AppendLine("<b>Updated transaction</b>");
                sb.AppendLine();

                AppendTransactions(
                    sb,
                    [updated.Transaction],
                    "<i>...and {0} more transaction(s) not shown.</i>");
                break;

            case DeleteTransactionsResponse.Success deleted:
                sb.AppendLine($"Deleted <b>{deleted.DeletedCount}</b> transaction(s)");
                break;

            case GetTransactionsResponse.Success listed:
                var totalListedAmount = listed.Transactions.Items.Sum(t =>
                    t.Type == TransactionType.Expense ? -t.Amount : t.Amount);
                var balanceSign = totalListedAmount > 0 ? "+" : string.Empty;

                var datePart = FormatDateRange(listed.FromDate, listed.ToDate);

                var header = listed.CategoryNames.Count > 0
                    ? $"<b>Transactions in '{WebUtility.HtmlEncode(string.Join(", ", listed.CategoryNames))}'</b> {datePart}"
                    : $"<b>Transactions</b> {datePart}";

                sb.AppendLine(header);
                sb.AppendLine($"Total: <code>{balanceSign}{totalListedAmount:0.00}</code>");
                sb.AppendLine();

                if (listed.Transactions.Items.Count == 0)
                {
                    sb.AppendLine("No transactions found.");
                    break;
                }

                AppendTransactions(
                    sb,
                    listed.Transactions.Items,
                    "<i>...and {0} more transaction(s) hidden. Please narrow down the date range or category to view more.</i>");
                break;

            case ListCategoriesResponse.Success listedCat:
                sb.AppendLine($"📋 <b>Available Categories ({listedCat.Categories.Count}):</b>");
                foreach (var c in listedCat.Categories)
                {
                    var aliases = c.Aliases.Count > 0
                        ? $" (aliases: <code>{WebUtility.HtmlEncode(string.Join(", ", c.Aliases))}</code>)"
                        : string.Empty;

                    sb.AppendLine(
                        $"   • <b>{WebUtility.HtmlEncode(c.Name)}</b>{aliases}");
                }
                break;

            case IValidationErrorResponse validationError:
                var validationHeader = !string.IsNullOrWhiteSpace(rawInput)
                    ? $"⚠️ <b>Validation Error(s)</b> for '<b>{WebUtility.HtmlEncode(rawInput)}</b>':"
                    : "⚠️ <b>Validation Error(s):</b>";

                sb.AppendLine(validationHeader);

                foreach (var err in validationError.Errors)
                {
                    sb.AppendLine($"• {WebUtility.HtmlEncode(FormatValidationError(err))}");
                }
                break;

            case IFailureResponse:
                var failureMessage = !string.IsNullOrWhiteSpace(rawInput)
                    ? $"❌ <b>Action Failed</b> for '<b>{WebUtility.HtmlEncode(rawInput)}</b>'"
                    : "❌ <b>Action Failed</b>";

                sb.AppendLine(failureMessage);
                break;

            default:
                sb.AppendLine(
                    $"• {WebUtility.HtmlEncode(response.ToString())}");
                break;
        }
    }

    private static void AppendTransactions(
        StringBuilder sb,
        IReadOnlyList<Transaction> transactions,
        string overflowTemplate)
    {
        var displayedCount = 0;

        foreach (var t in transactions)
        {
            if (displayedCount >= MaxDisplayedTransactions)
            {
                break;
            }

            var sign = t.Type == TransactionType.Expense ? "-" : "+";
            var itemSb = new StringBuilder();

            if (displayedCount > 0)
            {
                itemSb.AppendLine();
            }

            itemSb.AppendLine($"<b>{sign}{t.Amount:0.00}</b>");
            itemSb.AppendLine($"<b>{WebUtility.HtmlEncode(t.Category.Name)}</b>");

            if (!string.IsNullOrWhiteSpace(t.Note))
            {
                itemSb.AppendLine(WebUtility.HtmlEncode(t.Note));
            }

            itemSb.AppendLine($"{t.Date:dd.MM.yyyy HH:mm:ss}");
            itemSb.Append($"<code>{t.Id}</code>");

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
            sb.AppendLine(string.Format(overflowTemplate, remaining));
        }
    }

    private static string BuildInvalidInputMessage(ProcessingResult.InvalidInput invalid)
    {
        var sb = new StringBuilder();
        sb.AppendLine("⚠️ <b>Input Error</b>");
        sb.AppendLine(WebUtility.HtmlEncode(invalid.Details));

        return sb.ToString();
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
