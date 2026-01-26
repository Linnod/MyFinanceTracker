using MyFinanceTracker.Interactions.Contracts;
using MyFinanceTracker.Interactions.Interpretation;

namespace MyFinanceTracker.Interactions.Abstractions;

internal interface IInteractionHandler
{
    bool CanHandle(InteractionType type);
    Task<InteractionResponse> HandleAsync(string payload, CancellationToken ct);
}
