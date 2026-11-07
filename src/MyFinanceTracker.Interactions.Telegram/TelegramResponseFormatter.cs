using System.Diagnostics;
using System.Net;
using System.Text;
using MyFinanceTracker.Common.Notifications;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.InputProcessing;
using MyFinanceTracker.UseCases;

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

    private static string FormatCompleted(ProcessingResult.Completed completed)
    {
        var sb = new StringBuilder();

        foreach (var action in completed.Actions)
        {
            switch (action)
            {
                case ActionResult.Transaction.Added added:
                    var totalCreatedCount = added.Transactions.Count;

                    sb.AppendLine($"<b>Created {totalCreatedCount} transaction(s)</b>");
                    sb.AppendLine();

                    AppendTransactions(
                        sb,
                        added.Transactions,
                        "<i>...and {0} more transaction(s) not shown.</i>");
                    break;

                case ActionResult.Transaction.Deleted deleted:
                    sb.AppendLine($"Deleted <b>{deleted.DeletedCount}</b> transaction(s)");
                    break;

                case ActionResult.Transaction.Listed listed:
                    var totalListedAmount = listed.Transactions.Sum(t =>
                        t.Type == TransactionType.Expense ? -t.Amount : t.Amount);
                    var balanceSign = totalListedAmount > 0 ? "+" : string.Empty;

                    var datePart = listed.FromDate == listed.ToDate
                        ? $"for <code>{listed.FromDate:dd.MM.yyyy}</code>"
                        : $"from <code>{listed.FromDate:dd.MM.yyyy}</code> to <code>{listed.ToDate:dd.MM.yyyy}</code>";

                    var header = listed.CategoryNames.Count > 0
                        ? $"<b>Transactions in '{WebUtility.HtmlEncode(string.Join(", ", listed.CategoryNames))}'</b> {datePart}"
                        : $"<b>Transactions</b> {datePart}";

                    sb.AppendLine(header);
                    sb.AppendLine($"Total: <code>{balanceSign}{totalListedAmount:0.00}</code>");
                    sb.AppendLine();

                    if (listed.Transactions.Count == 0)
                    {
                        sb.AppendLine("No transactions found.");
                        break;
                    }

                    AppendTransactions(
                        sb,
                        listed.Transactions,
                        "<i>...and {0} more transaction(s) hidden. Please narrow down the date range or category to view more.</i>");
                    break;

                case ActionResult.Category.Listed listedCat:
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

                case ActionResult.InvalidInput input:
                    sb.AppendLine(
                        $"⚠️ <b>Validation Error(s)</b> for " +
                        $"'<b>{WebUtility.HtmlEncode(input.RawInput)}</b>':");

                    foreach (var err in input.Errors)
                    {
                        sb.AppendLine($"• {WebUtility.HtmlEncode(FormatValidationError(err))}");
                    }
                    break;

                case ActionResult.Failure failure:
                    sb.AppendLine(
                        $"❌ <b>Action Failed</b> for " +
                        $"'<b>{WebUtility.HtmlEncode(failure.RawInput)}</b>'");
                    break;

                default:
                    sb.AppendLine(
                        $"• {WebUtility.HtmlEncode(action.ToString())}");
                    break;
            }

            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
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

            itemSb.AppendLine($"{t.Date:dd.MM.yyyy}");
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
        _ => err.ErrorCode
    };
}
