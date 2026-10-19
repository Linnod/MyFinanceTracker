namespace MyFinanceTracker.InputProcessing;

public interface IInputReceiver
{
    Task<ProcessingResult> Receive(IInput input, CancellationToken ct);
}
