namespace MyFinanceTracker.InputProcessing.Text;

public interface ITextInputReceiver
{
    Task<ProcessingResult> Receive(TextInput input, CancellationToken ct);
}