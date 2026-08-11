using MyFinanceTracker.InputProcessing.Text.Regex.Dispatching.Commands;

namespace MyFinanceTracker.InputProcessing.Text.Regex.Interpretation;

internal sealed record TextCommandDomain(
    string Name, 
    string[] Aliases, 
    IReadOnlyDictionary<string, Func<string, ITextCommand>> Actions,
    IReadOnlyCollection<Type> CommandTypes);