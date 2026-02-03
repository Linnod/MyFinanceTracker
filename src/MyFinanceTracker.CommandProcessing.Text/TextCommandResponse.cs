namespace MyFinanceTracker.CommandProcessing.Text;

public abstract record TextCommandResponse
{
    private TextCommandResponse() { }

    public sealed record Success(
        string CommandDescription,
        string PrimaryValue,
        IReadOnlyCollection<TextCommandResponseDetail> Details
    ) : TextCommandResponse
    {
        public override string ToString() => $"{PrimaryValue} ({Details.Count} details)";
    }

    public sealed record InvalidInput(
        string Details,
        string? Suggestion = null,
        IReadOnlyCollection<string>? Examples = null) : TextCommandResponse
    {
        public override string ToString() =>
            $"REJECTED | {Details}" +
            (Suggestion != null ? $" | Suggestion: {Suggestion}" : "");
    }

    public sealed record LogicError(string Message) : TextCommandResponse
    {
        public override string ToString() => $"[BUSINESS LOGIC] REJECTED | {Message}";
    }

    public sealed record SystemError(string Message, Exception? Exception = null) : TextCommandResponse
    {
        public override string ToString() => $"[SYSTEM FAILURE] REJECTED | {Message}";
    }

    public override string ToString() => GetType().Name;
}

public record TextCommandResponseDetail(string Name, string Value, string? Icon = null);