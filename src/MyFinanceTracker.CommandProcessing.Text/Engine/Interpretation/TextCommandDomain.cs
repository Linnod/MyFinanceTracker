using MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands;

namespace MyFinanceTracker.CommandProcessing.Text.Engine.Interpretation;

internal sealed record TextCommandDomain(
    string Name, 
    string[] Aliases, 
    IReadOnlyDictionary<string, Func<string, ITextCommand>> Actions)
{
}
