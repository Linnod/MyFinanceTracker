namespace MyFinanceTracker.InputProcessing.Text.Regex.Dispatching.Commands;

internal interface ICommandHandler<in TCommand> where TCommand : ITextCommand
{
    Task<ActionResult> Handle(TCommand command, CancellationToken ct);
}