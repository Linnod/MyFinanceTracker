using MyFinanceTracker.InputProcessing.Text.Structured.Dispatching.Commands;

namespace MyFinanceTracker.InputProcessing.Text.Structured.Interpretation;

internal sealed record TextCommandDomain(
    string Name, 
    string[] Aliases, 
    IReadOnlyDictionary<string, Func<string, ITextCommand>> Actions,
    IReadOnlyCollection<Type> CommandTypes);