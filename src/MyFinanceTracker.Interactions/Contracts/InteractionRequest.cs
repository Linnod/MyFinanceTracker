using MediatR;
using MyFinanceTracker.Common.Contracts;

namespace MyFinanceTracker.Interactions.Contracts;

public record InteractionRequest(string Input) : IRequest<InteractionResponse>, ILoggableRequest
{
    string ILoggableRequest.GetLogPayload() => Input;
}
