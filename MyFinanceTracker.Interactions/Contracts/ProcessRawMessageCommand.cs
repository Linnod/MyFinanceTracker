using MediatR;
using MyFinanceTracker.Common.Contracts;

namespace MyFinanceTracker.Interactions.Contracts;

public record ProcessRawMessageCommand(string RawInput) : IRequest<InteractionResult>, ILoggableRequest
{
    string ILoggableRequest.GetLogPayload() => RawInput;
}