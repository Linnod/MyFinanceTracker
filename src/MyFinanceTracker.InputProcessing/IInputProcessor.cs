namespace MyFinanceTracker.InputProcessing;

public interface IInputProcessor<in TInput> where TInput : Input
{
    Task<ProcessingResult> Process(TInput input, CancellationToken ct);
}
