using System.Diagnostics;
using System.Net;
using System.Text;
using MyFinanceTracker.InputProcessing.Text;

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
                    var category = added.Transactions.FirstOrDefault()?.Category?.Name ?? "Unknown";
                    var total = added.Transactions.Sum(t => Math.Abs(t.Amount));

                    sb.AppendLine($"🔹 Created <b>{added.Transactions.Count}</b> transaction(s) in '<b>{WebUtility.HtmlEncode(category)}</b>'");
                    sb.AppendLine($"💰 Total: <code>{total}</code>");

                    foreach (var t in added.Transactions)
                    {
                        var note = string.IsNullOrWhiteSpace(t.Note) ? "" : $" ({WebUtility.HtmlEncode(t.Note)})";
                        sb.AppendLine($"   • <code>{t.Date:dd.MM.yyyy}</code>: <code>{t.Amount}</code>{note}");
                    }
                    break;

                case ActionResult.Transaction.Deleted deleted:
                    sb.AppendLine($"🗑️ Cleared category '<b>{WebUtility.HtmlEncode(deleted.CategoryName)}</b>' for <code>{deleted.Date:dd.MM.yyyy}</code>");
                    break;

                case ActionResult.Category.Listed listed:
                    sb.AppendLine($"📋 <b>Available Categories ({listed.Categories.Count}):</b>");
                    foreach (var c in listed.Categories)
                    {
                        var icon = c.IsIncome ? "💰" : "💸";
                        var aliases = c.Aliases.Count > 0
                            ? $" (aliases: <code>{WebUtility.HtmlEncode(string.Join(", ", c.Aliases))}</code>)"
                            : string.Empty;

                        sb.AppendLine($"   {icon} <b>{WebUtility.HtmlEncode(c.Name)}</b>{aliases}");
                    }
                    break;

                case ActionResult.InvalidSyntax syntax:
                    sb.AppendLine($"⚠️ <b>Syntax Error</b> for '<b>{WebUtility.HtmlEncode(syntax.RawInput)}</b>': <code>{WebUtility.HtmlEncode(syntax.ErrorCode)}</code>");
                    AppendSuggestionAndExamples(sb, syntax.Suggestion, syntax.Examples);
                    break;

                case ActionResult.InvalidInput input:
                    sb.AppendLine($"⚠️ <b>Validation Error(s)</b> for '<b>{WebUtility.HtmlEncode(input.RawInput)}</b>':");
                    foreach (var err in input.Errors)
                    {
                        sb.AppendLine($"• <code>{WebUtility.HtmlEncode(err.ErrorCode)}</code>");
                        if (err.Suggestion is not null)
                        {
                            sb.AppendLine($"   💡 Suggestion: <code>{WebUtility.HtmlEncode(err.Suggestion)}</code>");
                        }
                    }
                    break;

                case ActionResult.DomainError domainError:
                    sb.AppendLine($"⚠️ <b>Domain Error</b> for '<b>{WebUtility.HtmlEncode(domainError.RawInput)}</b>': <code>{WebUtility.HtmlEncode(domainError.ErrorCode)}</code>");
                    AppendSuggestionAndExamples(sb, domainError.Suggestion, domainError.Examples);
                    break;

                case ActionResult.Failure failure:
                    sb.AppendLine($"❌ <b>Action Failed</b> for '<b>{WebUtility.HtmlEncode(failure.RawInput)}</b>'");
                    break;

                default:
                    sb.AppendLine($"• {WebUtility.HtmlEncode(action.ToString())}");
                    break;
            }

            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private static void AppendSuggestionAndExamples(
        StringBuilder sb,
        string? suggestion,
        IReadOnlyCollection<string>? examples)
    {
        if (suggestion is not null)
        {
            sb.AppendLine($"💡 Did you mean: <code>{WebUtility.HtmlEncode(suggestion)}</code>?");
        }

        if (examples is { Count: > 0 })
        {
            sb.AppendLine("<b>Examples:</b>");
            foreach (var example in examples)
            {
                sb.AppendLine($"• <code>{WebUtility.HtmlEncode(example)}</code>");
            }
        }
    }

    private static string BuildInvalidInputMessage(ProcessingResult.InvalidInput invalid)
    {
        var sb = new StringBuilder();
        sb.AppendLine("⚠️ <b>Input Error</b>");

        sb.AppendLine(WebUtility.HtmlEncode(invalid.Details));
        sb.AppendLine();

        if (invalid.Suggestion is not null)
        {
            sb.AppendLine($"💡 Did you mean: <code>{WebUtility.HtmlEncode(invalid.Suggestion)}</code>?");
        }

        if (invalid.Examples is { Count: > 0 })
        {
            sb.AppendLine("<b>Try like this:</b>");
            foreach (var example in invalid.Examples)
            {
                sb.AppendLine($"• <code>{WebUtility.HtmlEncode(example)}</code>");
            }
        }

        return sb.ToString();
    }
}