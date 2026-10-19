namespace MyFinanceTracker.InputProcessing.Inputs;

public record TextInput(string Text) : IInput
{
    public override string ToString() => Text;

    public void Dispose() => GC.SuppressFinalize(this);
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}