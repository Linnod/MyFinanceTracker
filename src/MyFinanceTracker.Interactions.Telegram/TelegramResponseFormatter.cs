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
                            $"   • <code>{t.Id}</code> | " +
                            $"<code>{t.Date:dd.MM.yyyy}</code> | " +
                            $"<b>{WebUtility.HtmlEncode(t.Category.Name)}</b> | " +
                            $"<code>{sign}{t.Amount:0.00}</code>{note}");
                    }
                    break;

                case ActionResult.Transaction.Deleted deleted:
                    sb.AppendLine($"🗑️ Deleted <b>{deleted.DeletedCount}</b> transaction(s)");
                    break;

                case ActionResult.Transaction.Listed listed:
                    var totalListedAmount = listed.Transactions.Sum(t =>
                        t.Type == TransactionType.Expense ? -t.Amount : t.Amount);
                    var balanceSign = totalListedAmount > 0 ? "+" : string.Empty;

                    var datePart = listed.FromDate == listed.ToDate
                        ? $"for <code>{listed.FromDate:dd.MM.yyyy}</code>"
                        : $"from <code>{listed.FromDate:dd.MM.yyyy}</code> to <code>{listed.ToDate:dd.MM.yyyy}</code>";

                    var header = listed.CategoryNames.Count > 0
                        ? $"📋 <b>Transactions in '{WebUtility.HtmlEncode(string.Join(", ", listed.CategoryNames))}'</b> {datePart}"
                        : $"📋 <b>Transactions</b> {datePart}";

                    sb.AppendLine(header);
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
                            : $" ({WebUtility.HtmlEncode(t.Note)})";

                        sb.AppendLine(
                            $"   • <code>{t.Id}</code> | " +
                            $"<code>{t.Date:dd.MM.yyyy}</code> | " +
                            $"<b>{WebUtility.HtmlEncode(t.Category.Name)}</b> | " +
                            $"<code>{sign}{t.Amount:0.00}</code>{note}");
                    }

                    break;

                case ActionResult.Category.Listed listed:
                    sb.AppendLine($"📋 <b>Available Categories ({listed.Categories.Count}):</b>");
                    foreach (var c in listed.Categories)
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
