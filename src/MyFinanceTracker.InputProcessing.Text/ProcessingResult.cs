namespace MyFinanceTracker.InputProcessing.Text;

public abstract record ProcessingResult
{
    private ProcessingResult() { }

    public sealed record Completed(
        IReadOnlyList<ActionResult> Actions
    ) : ProcessingResult
    {
        public override string ToString() => $"COMPLETED ({Actions.Count} actions)";
    }

    public sealed record EmptyInput() : ProcessingResult
    {
        public override string ToString() => "EMPTY INPUT | Provided input is empty.";
    }

    public sealed record InvalidInput(
        string Details,
        string? Suggestion = null,
        IReadOnlyCollection<string>? Examples = null
    ) : ProcessingResult
    {
        public override string ToString() => $"INVALID INPUT | {Details}";
    }

    public sealed record LogicError(string Message) : ProcessingResult
    {
        public override string ToString() => $"[LOGIC ERROR] {Message}";
    }

    public sealed record SystemError(string Message, Exception? Exception = null) : ProcessingResult
    {
        public override string ToString() => $"[SYSTEM ERROR] {Message}";
    }

    public override string ToString() => GetType().Name;
}