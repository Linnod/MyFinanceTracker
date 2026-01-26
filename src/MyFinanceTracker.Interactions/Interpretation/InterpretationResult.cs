namespace MyFinanceTracker.Interactions.Interpretation;

internal abstract record InterpretationResult
{
    private InterpretationResult() { }

    public sealed record Identified(
        InteractionType Type, 
        string Payload
    ) : InterpretationResult;

    public sealed record Unrecognized : InterpretationResult;
}