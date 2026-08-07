namespace MyFinanceTracker.CommandProcessing.Text.Regex.Dispatching.Commands;

internal interface ICommandHandler<in TCommand> where TCommand : ITextCommand
{
    Task<TextCommandResponse> Handle(TCommand command, CancellationToken ct);
}