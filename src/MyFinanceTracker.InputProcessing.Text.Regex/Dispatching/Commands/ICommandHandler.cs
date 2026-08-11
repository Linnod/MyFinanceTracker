namespace MyFinanceTracker.InputProcessing.Text.Regex.Dispatching.Commands;

internal interface ICommandHandler<in TCommand> where TCommand : ITextCommand
{
    Task<CommandExecutionResult> Handle(TCommand command, CancellationToken ct);
}