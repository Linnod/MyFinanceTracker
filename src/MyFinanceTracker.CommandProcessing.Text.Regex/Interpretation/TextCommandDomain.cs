using MyFinanceTracker.CommandProcessing.Text.Regex.Dispatching.Commands;

namespace MyFinanceTracker.CommandProcessing.Text.Regex.Interpretation;

internal sealed record TextCommandDomain(
    string Name, 
    string[] Aliases, 
    IReadOnlyDictionary<string, Func<string, ITextCommand>> Actions)
{
}
