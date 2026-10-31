namespace MyFinanceTracker.InputProcessing;

public interface IInputReceiver
{
    Task<ProcessingResult> Receive(Input input, CancellationToken ct);
}
