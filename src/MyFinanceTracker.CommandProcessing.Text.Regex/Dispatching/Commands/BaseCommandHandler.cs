using MyFinanceTracker.UseCases;

namespace MyFinanceTracker.CommandProcessing.Text.Regex.Dispatching.Commands;

internal abstract class BaseCommandHandler
{
    protected static string FormatValidationErrors(IReadOnlyCollection<ValidationErrorItem> errors)
    {
        if (errors.Count == 1)
        {
            return errors.First().Message;
        }

        return string.Join("\n", errors.Select(e => $"• {e.Message}"));
    }
}