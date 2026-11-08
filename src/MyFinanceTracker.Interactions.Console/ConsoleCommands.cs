using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.InputProcessing;
using MyFinanceTracker.UseCases;
using MyFinanceTracker.UseCases.Category.List;
using MyFinanceTracker.UseCases.Transaction.Create;
using MyFinanceTracker.UseCases.Transaction.Delete;
using MyFinanceTracker.UseCases.Transaction.Get;
using MyFinanceTracker.UseCases.Transaction.Update;

namespace MyFinanceTracker.Interactions.Console;

internal static class ConsoleCommands
{
    public static void WriteCompleted(ProcessingResult.Completed completed)
    {
        System.Console.WriteLine();

        foreach (var action in completed.Actions)
        {
            switch (action.Response)
            {
                case CreateTransactionsResponse.Success added:
                    var totalCreated = added.Transactions.Count;
                    System.Console.WriteLine($"🔹 Created {totalCreated} transaction(s)");

                    foreach (var t in added.Transactions)
                    {
                        var sign = t.Type == TransactionType.Expense ? "-" : "+";
                        var note = string.IsNullOrWhiteSpace(t.Note) ? string.Empty : $" | {t.Note}";

                        System.Console.WriteLine(
                            $"  • {t.Id} | {t.Date:dd.MM.yyyy HH:mm:ss} | {t.Category.Name} | {sign}{t.Amount:0.00}{note}");
                    }
                    break;

                case UpdateTransactionResponse.Success updated:
                    System.Console.WriteLine("🔹 Updated transaction:");
                    {
                        var t = updated.Transaction;
                        var sign = t.Type == TransactionType.Expense ? "-" : "+";
                        var note = string.IsNullOrWhiteSpace(t.Note) ? string.Empty : $" | {t.Note}";

                        System.Console.WriteLine(
                            $"  • {t.Id} | {t.Date:dd.MM.yyyy HH:mm:ss} | {t.Category.Name} | {sign}{t.Amount:0.00}{note}");
                    }
                    break;

                case DeleteTransactionsResponse.Success deleted:
                    System.Console.WriteLine($"🗑️ Deleted {deleted.DeletedCount} transaction(s)");
                    break;

                case GetTransactionsResponse.Success listed:
                    var totalListedAmount = listed.Transactions.Items.Sum(t =>
                        t.Type == TransactionType.Expense ? -t.Amount : t.Amount);

                    var balanceSign = totalListedAmount > 0 ? "+" : "";
                    var datePart = FormatDateRange(listed.FromDate, listed.ToDate);

                    var categoriesPart = listed.CategoryNames.Count > 0
                        ? $" in '{string.Join(", ", listed.CategoryNames)}'"
                        : "";

                    System.Console.WriteLine(
                        $"📋 Transactions{categoriesPart} {datePart} (Total: {balanceSign}{totalListedAmount:0.00})");

                    if (listed.Transactions.Items.Count == 0)
                    {
                        System.Console.WriteLine("  • No transactions.");
                        break;
                    }

                    foreach (var t in listed.Transactions.Items)
                    {
                        var sign = t.Type == TransactionType.Expense ? "-" : "+";
                        var note = string.IsNullOrWhiteSpace(t.Note) ? string.Empty : $" | {t.Note}";

                        System.Console.WriteLine(
                            $"  • {t.Id} | {t.Date:dd.MM.yyyy HH:mm:ss} | {t.Category.Name} | {sign}{t.Amount:0.00}{note}");
                    }
                    break;

                case ListCategoriesResponse.Success listed:
                    System.Console.WriteLine($"📋 Listed {listed.Categories.Count} categories:");
                    foreach (var c in listed.Categories)
                    {
                        System.Console.WriteLine($"  • {c.Name} (Aliases: {string.Join(", ", c.Aliases)})");
                    }
                    break;

                case IValidationErrorResponse validationError:
                    WriteWarning($"⚠️ Validation Error(s) for '{action.RawInput}':");
                    System.Console.ForegroundColor = ConsoleColor.Yellow;
                    foreach (var err in validationError.Errors)
                    {
                        System.Console.WriteLine($"  • {FormatValidationError(err)}");
                    }
                    System.Console.ResetColor();
                    break;

                case IFailureResponse:
                    WriteError($"Action for '{action.RawInput}' failed due to pipeline processing failure.");
                    break;

                default:
                    System.Console.WriteLine($"• {action.Response}");
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
