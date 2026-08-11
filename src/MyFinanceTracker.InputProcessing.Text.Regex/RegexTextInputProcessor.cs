using System.Diagnostics;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.InputProcessing.Text.Regex.Dispatching;
using MyFinanceTracker.InputProcessing.Text.Regex.Interpretation;

namespace MyFinanceTracker.InputProcessing.Text.Regex;

internal sealed partial class RegexTextInputProcessor(
    ITextCommandInterpreter interpreter,
    ITextCommandDispatcher dispatcher,
    ILogger<RegexTextInputProcessor> logger) : ITextInputProcessor
{
    public async Task<ProcessingResult> Process(TextInput input, CancellationToken ct)
    {
        LogExecuteEntry(input);

        var lines = input.Text?.Split(['\n', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
        if (lines.Length == 0)
        {
            var empty = new ProcessingResult.EmptyInput();
            LogExecuteExit(empty);
            return empty;
        }

        var actions = new List<ActionResult>(lines.Length);
        foreach (var line in lines)
        {
            var action = await ProcessLine(line, ct);
            actions.Add(action);
        }

        var result = new ProcessingResult.Completed(actions);
        LogExecuteExit(result);

        return result;
    }

    private async Task<ActionResult> ProcessLine(string line, CancellationToken ct)
    {
        var input = InterpretationInput.Create(line);
        var interpretation = await interpreter.Interpret(input);

        return interpretation switch
        {
            InterpretationResult.Identified identified =>
                await dispatcher.Dispatch(identified.Command, ct),

            InterpretationResult.Unrecognized unrecognized =>
                new ActionResult.InvalidSyntax(
                    ErrorCode: unrecognized.ErrorCode,
                    Suggestion: unrecognized.Suggestion,
                    Examples: unrecognized.Examples),

            _ => throw new UnreachableException($"Interpretation result {interpretation.GetType().Name} was not handled.")
        };
    }
}