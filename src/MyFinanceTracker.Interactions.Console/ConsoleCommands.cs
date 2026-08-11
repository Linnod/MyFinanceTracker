using MyFinanceTracker.InputProcessing.Text;

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
                    var total = added.Transactions.Sum(t => Math.Abs(t.Amount));
                    var category = added.Transactions.FirstOrDefault()?.Category?.Name ?? "Unknown";

                    System.Console.WriteLine($"🔹 Created {added.Transactions.Count} transaction(s) in '{category}' (Total: {total})");
                    foreach (var t in added.Transactions)
                    {
                        System.Console.WriteLine($"  • {t.Date:dd.MM.yyyy} | {t.Amount} | {t.Note}");
                    }
                    break;

                case ActionResult.Transaction.Deleted deleted:
                    System.Console.WriteLine($"🗑️ Deleted transactions for '{deleted.CategoryName}' on {deleted.Date:dd.MM.yyyy}");
                    break;

                case ActionResult.Category.Listed listed:
                    System.Console.WriteLine($"📋 Listed {listed.Categories.Count} categories:");
                    foreach (var c in listed.Categories)
                    {
                        var icon = c.IsIncome ? "💰" : "💸";
                        System.Console.WriteLine($"  {icon} {c.Name} (Aliases: {string.Join(", ", c.Aliases)})");
                    }
                    break;

                case ActionResult.InvalidSyntax syntax:
                    WriteWarning($"⚠️ Syntax Error: {syntax.ErrorCode}");
                    WriteSuggestionAndExamples(syntax.Suggestion, syntax.Examples);
                    break;

                case ActionResult.InvalidInput input:
                    WriteWarning("⚠️ Validation Error(s):");
                    System.Console.ForegroundColor = ConsoleColor.Yellow;
                    foreach (var err in input.Errors)
                    {
                        System.Console.WriteLine($"  • {err.ErrorCode}");
                        if (err.Suggestion is not null)
                        {
                            System.Console.WriteLine($"    💡 Suggestion: {err.Suggestion}");
                        }
                    }
                    System.Console.ResetColor();
                    break;

                case ActionResult.DomainError domainError:
                    WriteWarning($"⚠️ Domain Error: {domainError.ErrorCode}");
                    WriteSuggestionAndExamples(domainError.Suggestion, domainError.Examples);
                    break;

                case ActionResult.Failure:
                    WriteError("Action failed due to pipeline processing failure.");
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

    private static void WriteSuggestionAndExamples(string? suggestion, IReadOnlyCollection<string>? examples)
    {
        System.Console.ForegroundColor = ConsoleColor.Yellow;

        if (suggestion is not null)
        {
            System.Console.WriteLine($"💡 Did you mean: '{suggestion}'?");
        }

        if (examples is { Count: > 0 })
        {
            System.Console.WriteLine("💡 Examples:");
            foreach (var example in examples)
            {
                System.Console.WriteLine($"  > {example}");
            }
        }

        System.Console.ResetColor();
    }
}