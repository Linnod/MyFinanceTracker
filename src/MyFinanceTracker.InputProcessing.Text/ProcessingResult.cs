namespace MyFinanceTracker.InputProcessing.Text;

public abstract record ProcessingResult
{
    private ProcessingResult() { }

    public sealed record Completed(
        IReadOnlyList<ActionResult> Actions
    ) : ProcessingResult
    {
        public override string ToString() => $"Completed ({Actions.Count} actions)";
    }

    public sealed record EmptyInput() : ProcessingResult
    {
        public override string ToString() => "EmptyInput";
    }

    public sealed record InvalidInput(
        string Details,
        string? Suggestion = null,
        IReadOnlyCollection<string>? Examples = null
    ) : ProcessingResult
    {
        public override string ToString() => $"InvalidInput: {Details}";
    }

    public sealed record SystemError(string Message, Exception? Exception = null) : ProcessingResult
    {
        public override string ToString() => $"SystemError: {Message}";
    }

    public override string ToString() => GetType().Name;
}