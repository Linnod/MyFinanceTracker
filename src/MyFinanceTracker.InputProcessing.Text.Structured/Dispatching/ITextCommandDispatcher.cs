using MyFinanceTracker.InputProcessing.Text.Structured.Dispatching.Commands;

namespace MyFinanceTracker.InputProcessing.Text.Structured.Dispatching;

internal interface ITextCommandDispatcher
{
    Task<CommandExecutionResult> Dispatch(ITextCommand command, CancellationToken ct);
}