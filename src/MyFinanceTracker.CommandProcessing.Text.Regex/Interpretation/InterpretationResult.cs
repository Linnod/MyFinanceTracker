using MyFinanceTracker.CommandProcessing.Text.Regex.Dispatching.Commands;

namespace MyFinanceTracker.CommandProcessing.Text.Regex.Interpretation;

internal abstract record InterpretationResult
{
    private InterpretationResult() { }

    public sealed record Identified(ITextCommand Command) : InterpretationResult
    {
        public override string ToString() => $"Identified {Command.GetType().Name}";
    }

    public sealed record EmptyInput : InterpretationResult
    {
        public override string ToString() => "Input is empty";
    }

    public sealed record Unrecognized(
        string Command, 
        string? Suggestion = null, 
        IReadOnlyCollection<string>? Examples = null) : InterpretationResult
    {
        public override string ToString() => $"[Command Engine] UNRECOGNIZED | Input: '{Command}'";
    }
}