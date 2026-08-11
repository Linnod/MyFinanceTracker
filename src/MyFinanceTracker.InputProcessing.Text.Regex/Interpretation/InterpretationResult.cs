using MyFinanceTracker.InputProcessing.Text.Regex.Dispatching.Commands;

namespace MyFinanceTracker.InputProcessing.Text.Regex.Interpretation;

internal abstract record InterpretationResult
{
    private InterpretationResult() { }

    public sealed record Identified(ITextCommand Command) : InterpretationResult
    {
        public override string ToString() => $"{nameof(Identified)} {Command.GetType().Name}";
    }

    public sealed record Unrecognized(
        string Input,
        string ErrorCode,
        string? Suggestion = null,
        IReadOnlyCollection<string>? Examples = null
    ) : InterpretationResult
    {
        public override string ToString() => $"{nameof(Unrecognized)} {Input}: {ErrorCode}";
    }
}