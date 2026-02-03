namespace MyFinanceTracker.CommandProcessing.Text.Engine.Interpretation;

internal abstract record InterpretationResult
{
    private InterpretationResult() { }

    public sealed record Identified(TextCommandType Type, string Payload) : InterpretationResult
    {
        public override string ToString() => $"Identified {Type}";
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