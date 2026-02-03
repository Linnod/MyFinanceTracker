using MyFinanceTracker.UseCases;

namespace MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands;

internal abstract class BaseCommandHandler : ICommandHandler
{
    public bool CanHandle(TextCommandType type) => type == GetHandlingCommandType();

    public abstract Task<TextCommandResponse> Handle(string payload, CancellationToken ct);

    public override string ToString() => GetType().Name;

    protected abstract TextCommandType GetHandlingCommandType();

    protected abstract string CommandName { get; }

    protected static string FormatValidationErrors(IReadOnlyCollection<ValidationErrorItem> errors)
    {
        if (errors.Count == 1)
        {
            return errors.First().Message;
        }

        return string.Join("\n", errors.Select(e => $"• {e.Message}"));
    }
}
