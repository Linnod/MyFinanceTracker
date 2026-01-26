using MyFinanceTracker.Interactions.Contracts;

namespace MyFinanceTracker.Interactions.Console;

internal static class ConsoleCommands
{
    public static void WriteSuccess(InteractionResponse.Success success)
    {
        System.Console.ForegroundColor = ConsoleColor.Green;
        System.Console.WriteLine($"\n✅ {success.InteractionDescription.ToUpper()}");
        System.Console.WriteLine($"Result: {success.PrimaryValue}");

        System.Console.ResetColor();
        foreach (var detail in success.Details)
        {
            var icon = detail.Icon ?? "•";
            System.Console.WriteLine($"{icon} {detail.Name}: {detail.Value}");
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
}