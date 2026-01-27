namespace MyFinanceTracker.Interactions.Contracts;

public interface IInteractionGateway
{
    Task<InteractionResponse> Send(InteractionRequest request, CancellationToken ct = default);
}
