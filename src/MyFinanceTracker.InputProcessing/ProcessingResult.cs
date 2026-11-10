using MyFinanceTracker.UseCases;

namespace MyFinanceTracker.InputProcessing;

public abstract record ProcessingResult
{
    private ProcessingResult() { }

    public sealed record Completed(
        IReadOnlyList<UseCaseResponse> Actions
    ) : ProcessingResult
    {
        public override string ToString() => $"Completed ({Actions.Count} actions)";
    }

    public sealed record InvalidInput(string Details) : ProcessingResult
    {
        public override string ToString() => $"InvalidInput: {Details}";
    }

    public sealed record SystemError(string Message) : ProcessingResult
    {
        public override string ToString() => $"SystemError: {Message}";
    }

    public override string ToString() => GetType().Name;
}
