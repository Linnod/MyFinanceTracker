namespace MyFinanceTracker.CommandProcessing.Text.Engine;

internal interface ITextCommandProcessor
{
    Task<TextCommandResponse> Execute(TextCommandRequest request, CancellationToken ct = default);
}