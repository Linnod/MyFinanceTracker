namespace MyFinanceTracker.InputProcessing;

public interface IInputProcessor<in TInput> where TInput : IInput
{
    Task<ProcessingResult> Process(TInput input, CancellationToken ct);
}