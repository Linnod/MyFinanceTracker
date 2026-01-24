using MediatR;
using MyFinanceTracker.Interactions.Contracts.Internal;

namespace MyFinanceTracker.Interactions.Contracts;

public record ProcessRawMessageCommand(string RawInput) : IRequest<InteractionResult>, ILoggableRequest
{
    string ILoggableRequest.GetLogPayload() => RawInput;
}