namespace MyFinanceTracker.InputProcessing.Text.Structured.Dispatching.Commands;

internal static class TextCommandExtensions
{
    public static CommandMetadataAttribute GetMetadata(this Type commandType)
    {
        return commandType
            .GetCustomAttributes(typeof(CommandMetadataAttribute), false)
            .FirstOrDefault() as CommandMetadataAttribute 
            ?? throw new InvalidOperationException($"Command {commandType.Name} is missing [CommandMetadata] attribute.");
    }

    public static CommandMetadataAttribute GetMetadata(this ITextCommand command)
        => command.GetType().GetMetadata();
}