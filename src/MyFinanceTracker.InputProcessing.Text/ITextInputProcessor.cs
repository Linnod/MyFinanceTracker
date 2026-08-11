namespace MyFinanceTracker.InputProcessing.Text;

public interface ITextInputProcessor
{
    Task<ProcessingResult> Process(TextInput input, CancellationToken ct);
}