namespace MyFinanceTracker.CommandProcessing.Text;

public interface ITextCommandReceiver
{
    Task<TextCommandResponse> Receive(TextCommandRequest request, CancellationToken ct = default);
}
