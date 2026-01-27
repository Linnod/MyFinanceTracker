using Microsoft.Extensions.DependencyInjection;
using MediatR;
using MyFinanceTracker.Interactions.Contracts;

namespace MyFinanceTracker.Interactions.Gateways;

internal sealed class InteractionGateway(IServiceScopeFactory scopeFactory) : IInteractionGateway
{
    public async Task<InteractionResponse> Send(InteractionRequest request, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        
        return await mediator.Send(request, ct);
    }
}