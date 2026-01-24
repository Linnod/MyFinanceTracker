using MyFinanceTracker.Interactions.Contracts;

namespace MyFinanceTracker.Interactions.Console;

internal class ConsoleCommands
{
    public static void WriteSuccess(FinancialOperation op)
    {
        System.Console.ForegroundColor = ConsoleColor.Green;
        var amountsDisplay = string.Join(" + ", op.Amounts);
        System.Console.WriteLine($"[SUCCESS] {op.Type} in '{op.CategoryAlias}' for {amountsDisplay} on {op.Date:dd.MM.yyyy}");
    }

    public static void WriteError(string error)
    {
        System.Console.ForegroundColor = ConsoleColor.Red;
        System.Console.WriteLine($"[ERROR] {error}");
    }

    public static void WriteInfo(string message)
    {
        System.Console.ForegroundColor = ConsoleColor.Cyan;
        System.Console.WriteLine(message);
    }
}
