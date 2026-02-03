using Microsoft.Extensions.Logging;
using MyFinanceTracker.Common.Utilities;

namespace MyFinanceTracker.CommandProcessing.Text.Engine.Interpretation;

internal sealed partial class StrictTextCommandInterpreter(ILogger<StrictTextCommandInterpreter> logger)
    : ITextCommandInterpreter
{
    private static readonly Dictionary<string, TextCommandType> CommandMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "add", TextCommandType.AddTransaction },
        { "new", TextCommandType.AddTransaction },
        { "+", TextCommandType.AddTransaction },
        { "добавить", TextCommandType.AddTransaction },

        { "rem", TextCommandType.DeleteTransaction },
        { "del", TextCommandType.DeleteTransaction },
        { "delete", TextCommandType.DeleteTransaction },
        { "remove", TextCommandType.DeleteTransaction },
        { "удалить", TextCommandType.DeleteTransaction },
        { "-", TextCommandType.DeleteTransaction }
    };

    private static readonly string[] CommandExamples = [.. CommandMap
        .GroupBy(x => x.Value)
        .Select(g => g.First().Key)];

    public Task<InterpretationResult> Interpret(string input)
    {
        LogInterpretationStarted(input);

        return Task.FromResult(InternalInterpret(input));
    }

    private InterpretationResult InternalInterpret(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            var result = new InterpretationResult.EmptyInput();
            LogEmptyInput(result);
            return result;
        }

        var parts = input.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var commandCandidate = parts[0];

        if (!CommandMap.TryGetValue(commandCandidate, out var type))
        {
            var suggestion = FuzzyMatcher.GetClosest(commandCandidate, CommandMap.Keys);
            var result = new InterpretationResult.Unrecognized(commandCandidate, suggestion, CommandExamples);
            LogUnrecognizedCommand(result);

            return result;
        }

        var payload = parts.Length > 1 ? parts[1].Trim() : string.Empty;
        var success = new InterpretationResult.Identified(type, payload);

        LogInterpretationSuccess(success);

        return success;
    }
}