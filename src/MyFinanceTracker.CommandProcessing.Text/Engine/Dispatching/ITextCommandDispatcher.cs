using MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands;

namespace MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching;

internal interface ITextCommandDispatcher
{
    Task<TextCommandResponse> Dispatch(ITextCommand command, CancellationToken ct = default);
}