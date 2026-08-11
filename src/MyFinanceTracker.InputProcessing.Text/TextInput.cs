namespace MyFinanceTracker.InputProcessing.Text;

public record TextInput(string Text)
{
    public override string ToString() => Text;
}