namespace MyFinanceTracker.InputProcessing.Text.Structured.Interpretation;

internal readonly record struct InterpretationInput
{
    public string Value { get; }

    private InterpretationInput(string value)
    {
        Value = value;
    }

    public static InterpretationInput Create(string raw)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(raw);
        
        return new InterpretationInput(raw.Trim());
    }

    public override string ToString() => Value;
}