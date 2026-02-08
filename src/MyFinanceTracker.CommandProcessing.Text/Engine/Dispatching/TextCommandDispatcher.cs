using Microsoft.Extensions.Logging;
using MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands;

namespace MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching;

internal sealed partial class TextCommandDispatcher(
    IEnumerable<ICommandHandler> handlers,
    ILogger<TextCommandDispatcher> logger) : ITextCommandDispatcher
{
    public async Task<TextCommandResponse> Dispatch(
        TextCommandType type,
        string payload,
        CancellationToken ct = default)
    {
        LogDispatchStarted(type, payload);

        var handler = handlers.FirstOrDefault(h => h.CanHandle(type));
        if (handler == null)
        {
            LogHandlerNotFound(type);

            return new TextCommandResponse.LogicError("This command is recognized but currently not supported.");
        }

        LogHandlerFound(handler);
        return await handler.Handle(payload, ct);
    }
}