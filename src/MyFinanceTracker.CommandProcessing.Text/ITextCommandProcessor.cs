namespace MyFinanceTracker.CommandProcessing.Text;

public interface ITextCommandProcessor
{
    Task<TextCommandResponse> Execute(TextCommandRequest request, CancellationToken ct = default);
}