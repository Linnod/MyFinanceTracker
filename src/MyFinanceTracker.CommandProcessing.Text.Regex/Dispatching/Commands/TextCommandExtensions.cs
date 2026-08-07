namespace MyFinanceTracker.CommandProcessing.Text.Regex.Dispatching.Commands;

internal static class TextCommandExtensions
{
    public static CommandMetadataAttribute GetMetadata(this ITextCommand command)
    {
        return command.GetType()
            .GetCustomAttributes(typeof(CommandMetadataAttribute), false)
            .FirstOrDefault() as CommandMetadataAttribute 
            ?? throw new InvalidOperationException($"Command {command.GetType().Name} is missing [CommandMetadata] attribute.");
    }
}