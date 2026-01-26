using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.Interactions.Interpretation;

internal class StrictInteractionInterpreter(ILogger<StrictInteractionInterpreter> logger)
    : IInteractionInterpreter
{
    private static readonly Dictionary<string, InteractionType> CommandMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "add", InteractionType.AddTransaction },
        { "rem", InteractionType.DeleteTransaction }
    };

    public InterpretationResult Interpret(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            logger.LogWarning("Interpretation failed: input is null or empty.");

            return new InterpretationResult.Unrecognized();
        }

        var parts = input.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var commandCandidate = parts[0];
        if (!CommandMap.TryGetValue(commandCandidate, out var type))
        {
            logger.LogWarning("Interpretation failed: command '{Command}' is not recognized.", commandCandidate);

            return new InterpretationResult.Unrecognized();
        }

        var payload = parts.Length > 1 ? parts[1].Trim() : string.Empty;

        return new InterpretationResult.Identified(type, payload);
    }
}
