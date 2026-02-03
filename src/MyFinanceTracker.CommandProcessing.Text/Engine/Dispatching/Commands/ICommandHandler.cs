namespace MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands;

internal interface ICommandHandler
{
    bool CanHandle(TextCommandType type);
    Task<TextCommandResponse> Handle(string payload, CancellationToken ct);
}
