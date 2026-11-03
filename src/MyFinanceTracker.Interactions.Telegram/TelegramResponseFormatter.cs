using System.Diagnostics;
using System.Net;
using System.Text;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.InputProcessing;
using MyFinanceTracker.UseCases;

namespace MyFinanceTracker.Interactions.Telegram;

internal static class TelegramResponseFormatter
{
    public static string FormatResponse(ProcessingResult response) => response switch
    {
        ProcessingResult.Completed completed => FormatCompleted(completed),

        ProcessingResult.EmptyInput => "ℹ️ <i>Received empty input.</i>",

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

                    sb.AppendLine(
                        $"🔹 Created <b>{totalCreatedCount}</b> transaction(s)");

                    foreach (var t in added.Transactions)
                    {
                        var sign = t.Type == TransactionType.Expense ? "-" : "+";
                        var note = string.IsNullOrWhiteSpace(t.Note)
                            ? string.Empty
                            : $" ({WebUtility.HtmlEncode(t.Note)})";

                        sb.AppendLine(
                            $"   • <code>{t.Date:dd.MM.yyyy}</code> | " +
                            $"<b>{WebUtility.HtmlEncode(t.Category.Name)}</b> | " +
                            $"<code>{sign}{t.Amount:0.00}</code>{note}");
                    }
                    break;

                case ActionResult.Transaction.Deleted deleted:
                    sb.AppendLine(
                        $"🗑️ Cleared category '<b>{WebUtility.HtmlEncode(deleted.CategoryName)}</b>' " +
                        $"for <code>{deleted.Date:dd.MM.yyyy}</code>");
                    break;

                case ActionResult.Transaction.Listed listed:
                    var totalListedAmount = listed.Transactions.Sum(t =>
                        t.Type == TransactionType.Expense ? -t.Amount : t.Amount);
                    var balanceSign = totalListedAmount >= 0 ? "+" : string.Empty;

                    sb.AppendLine(
                        $"📋 <b>Transactions in '{WebUtility.HtmlEncode(listed.CategoryName)}'</b> " +
                        $"for <code>{listed.Date:dd.MM.yyyy}</code>");
                    sb.AppendLine($"💰 Total: <code>{balanceSign}{totalListedAmount:0.00}</code>");

                    if (listed.Transactions.Count == 0)
                    {
                        sb.AppendLine("   • No transactions.");
                        break;
                    }

                    foreach (var t in listed.Transactions)
                    {
                        var sign = t.Type == TransactionType.Expense ? "-" : "+";
                        var note = string.IsNullOrWhiteSpace(t.Note)
                            ? string.Empty
                            : $" | {WebUtility.HtmlEncode(t.Note)}";

                        sb.AppendLine($"   • <code>{sign}{t.Amount:0.00}</code>{note}");
                    }

                    break;

                case ActionResult.Category.Listed listed:
                    sb.AppendLine($"📋 <b>Available Categories ({listed.Categories.Count}):</b>");
                    foreach (var c in listed.Categories)
                    {
                        var icon = c.IsIncome ? "💰" : "💸";
                        var aliases = c.Aliases.Count > 0
                            ? $" (aliases: <code>{WebUtility.HtmlEncode(string.Join(", ", c.Aliases))}</code>)"
                            : string.Empty;

                        sb.AppendLine(
                            $"   {icon} <b>{WebUtility.HtmlEncode(c.Name)}</b>{aliases}");
                    }
                    break;

                case ActionResult.InvalidInput input:
                    sb.AppendLine(
                        $"⚠️ <b>Validation Error(s)</b> for " +
                        $"'<b>{WebUtility.HtmlEncode(input.RawInput)}</b>':");

                    foreach (var err in input.Errors)
                    {
                        sb.AppendLine($"• {WebUtility.HtmlEncode(FormatValidationError(err.ErrorCode))}");

                        if (err.Suggestion is not null)
                        {
                            sb.AppendLine(
                                $"   💡 Suggestion: " +
                                $"<code>{WebUtility.HtmlEncode(err.Suggestion)}</code>");
                        }
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

    private static string BuildInvalidInputMessage(ProcessingResult.InvalidInput invalid)
    {
        var sb = new StringBuilder();
        sb.AppendLine("⚠️ <b>Input Error</b>");
        sb.AppendLine(WebUtility.HtmlEncode(invalid.Details));

        return sb.ToString();
    }
    private static string FormatValidationError(string errorCode) => errorCode switch
    {
        ValidationErrorCode.Transaction.CategoryRequired => "Category is required.",
        ValidationErrorCode.Transaction.CategoryNotFound => "Category was not found.",
        ValidationErrorCode.Transaction.DateRequired => "Date is required.",
        ValidationErrorCode.Transaction.DateOutOfRange => "Date is out of the allowed range.",
        ValidationErrorCode.Transaction.AmountRequired => "At least one amount is required.",
        ValidationErrorCode.Transaction.AmountMustBePositive => "Amount must be greater than zero.",
        _ => errorCode
    };
}
