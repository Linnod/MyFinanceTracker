namespace MyFinanceTracker.CommandProcessing.Text.Engine.Interpretation;

internal sealed record TextCommandDomain(
    string Name, 
    string[] Aliases, 
    IReadOnlyDictionary<string, TextCommandType> Actions)
{
}
