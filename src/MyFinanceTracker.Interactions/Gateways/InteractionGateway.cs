using Microsoft.Extensions.DependencyInjection;
using MediatR;
using MyFinanceTracker.Interactions.Contracts;
using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.Interactions.Gateways;

internal sealed class InteractionGateway(
    IServiceScopeFactory scopeFactory,
    ILogger<InteractionGateway> logger) : IInteractionGateway
{
    public async Task<InteractionResponse> Send(InteractionRequest request, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var correlationId = Guid.NewGuid();
        using (logger.BeginScope("ID: {CorrelationId}", correlationId))
        {
            logger.LogInformation("--> Send");

            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var response = await mediator.Send(request, ct);

            logger.LogInformation("<-- Send");

            return response;
        }
    }
}