using MyFinanceTracker.UseCases;

namespace MyFinanceTracker.InputProcessing;

public sealed record ActionResult(
    string RawInput,
    UseCaseResponse Response);
