using MyFinanceTracker.InputProcessing.Text.Regex.Dispatching.Commands;

namespace MyFinanceTracker.InputProcessing.Text.Regex.Dispatching;

internal interface ITextCommandDispatcher
{
    Task<CommandExecutionResult> Dispatch(ITextCommand command, CancellationToken ct);
}