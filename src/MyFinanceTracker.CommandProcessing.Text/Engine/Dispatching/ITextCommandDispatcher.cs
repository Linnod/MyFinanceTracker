namespace MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching;

internal interface ITextCommandDispatcher
{
    Task<TextCommandResponse> Dispatch(
        TextCommandType type, 
        string payload, 
        CancellationToken ct = default);
}