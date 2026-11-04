using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.InputProcessing;
using MyFinanceTracker.UseCases;

namespace MyFinanceTracker.Interactions.Console;

internal static class ConsoleCommands
{
    public static void WriteCompleted(ProcessingResult.Completed completed)
    {
        System.Console.WriteLine();

        foreach (var action in completed.Actions)
        {
            switch (action)
            {
                case ActionResult.Transaction.Added added:
                    var totalCreated = added.Transactions.Count;
                    System.Console.WriteLine($"🔹 Created {totalCreated} transaction(s)");

                    foreach (var t in added.Transactions)
                    {
                        var sign = t.Type == TransactionType.Expense ? "-" : "+";
                        var note = string.IsNullOrWhiteSpace(t.Note) ? string.Empty : $" | {t.Note}";

                        System.Console.WriteLine(
                            $"  • {t.Id} | {t.Date:dd.MM.yyyy} | {t.Category.Name} | {sign}{t.Amount:0.00}{note}");
                    }
                    break;

                case ActionResult.Transaction.Deleted deleted:
                    System.Console.WriteLine($"🗑️ Deleted {deleted.DeletedCount} transaction(s)");
                    break;

                case ActionResult.Transaction.Listed listed:
                    var totalListedAmount = listed.Transactions.Sum(t =>
                        t.Type == TransactionType.Expense ? -t.Amount : t.Amount);

                    var balanceSign = totalListedAmount > 0 ? "+" : "";

                    var datePart = listed.FromDate == listed.ToDate
                        ? $"for {listed.FromDate:dd.MM.yyyy}"
                        : $"from {listed.FromDate:dd.MM.yyyy} to {listed.ToDate:dd.MM.yyyy}";

                    var categoriesPart = listed.CategoryNames.Count > 0
                        ? $" in '{string.Join(", ", listed.CategoryNames)}'"
                        : "";

                    System.Console.WriteLine(
                        $"📋 Transactions{categoriesPart} {datePart} (Total: {balanceSign}{totalListedAmount:0.00})");

                    if (listed.Transactions.Count == 0)
                    {
                        System.Console.WriteLine("  • No transactions.");
                        break;
                    }

                    foreach (var t in listed.Transactions)
                    {
                        var sign = t.Type == TransactionType.Expense ? "-" : "+";
                        var note = string.IsNullOrWhiteSpace(t.Note) ? string.Empty : $" | {t.Note}";

                        System.Console.WriteLine(
                            $"  • {t.Id} | {t.Date:dd.MM.yyyy} | {t.Category.Name} | {sign}{t.Amount:0.00}{note}");
                    }
                    break;

                case ActionResult.Category.Listed listed:
                    System.Console.WriteLine($"📋 Listed {listed.Categories.Count} categories:");
                    foreach (var c in listed.Categories)
                    {
                        System.Console.WriteLine($"  • {c.Name} (Aliases: {string.Join(", ", c.Aliases)})");
                    }
                    break;

                case ActionResult.InvalidInput input:
                    WriteWarning($"⚠️ Validation Error(s) for '{input.RawInput}':");
                    System.Console.ForegroundColor = ConsoleColor.Yellow;
                    foreach (var err in input.Errors)
                    {
                        System.Console.WriteLine($"  • {FormatValidationError(err)}");
                    }
                    System.Console.ResetColor();
                    break;

                case ActionResult.Failure failure:
                    WriteError($"Action for '{failure.RawInput}' failed due to pipeline processing failure.");
                    break;

                default:
                    System.Console.WriteLine($"• {action}");
                    break;
            }
        }
    }

    public static void WriteError(string error)
    {
        System.Console.ForegroundColor = ConsoleColor.Red;
        System.Console.WriteLine($"\n[ERROR] {error}");
        System.Console.ResetColor();
    }

    public static void WriteInfo(string message)
    {
        System.Console.ForegroundColor = ConsoleColor.Cyan;
        System.Console.WriteLine(message);
        System.Console.ResetColor();
    }

    public static async Task<string?> ReadLine(CancellationToken ct)
    {
        try
        {
            while (!System.Console.KeyAvailable)
            {
                await Task.Delay(100, ct);
            }

            return System.Console.ReadLine();
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    private static void WriteWarning(string message)
    {
        System.Console.ForegroundColor = ConsoleColor.Yellow;
        System.Console.WriteLine(message);
        System.Console.ResetColor();
    }
    private static string FormatValidationError(ValidationErrorItem err) => err.ErrorCode switch
    {
        ValidationErrorCode.Transaction.CategoryRequired => "Category is required.",
        ValidationErrorCode.Transaction.CategoryNotFound => err.AttemptedValue is not null
            ? $"Category '{err.AttemptedValue}' was not found."
            : "Category was not found.",
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
