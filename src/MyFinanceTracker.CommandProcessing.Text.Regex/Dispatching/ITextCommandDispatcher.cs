using MyFinanceTracker.CommandProcessing.Text.Regex.Dispatching.Commands;

namespace MyFinanceTracker.CommandProcessing.Text.Regex.Dispatching;

internal interface ITextCommandDispatcher
{
    Task<TextCommandResponse> Dispatch(ITextCommand command, CancellationToken ct = default);
}