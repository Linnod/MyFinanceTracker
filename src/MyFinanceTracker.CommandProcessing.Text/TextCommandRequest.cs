namespace MyFinanceTracker.CommandProcessing.Text;

public record TextCommandRequest(string Input)
{
    public override string ToString() => Input;
}