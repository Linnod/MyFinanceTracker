using MediatR;

namespace MyFinanceTracker.Interactions.Contracts;

public record ProcessRawMessageCommand(string RawInput) : IRequest<InteractionResult>;
