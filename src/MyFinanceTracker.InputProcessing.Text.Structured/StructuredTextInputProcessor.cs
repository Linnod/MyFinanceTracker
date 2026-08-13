using System.Diagnostics;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.InputProcessing.Text.Structured.Dispatching;
using MyFinanceTracker.InputProcessing.Text.Structured.Dispatching.Commands;
using MyFinanceTracker.InputProcessing.Text.Structured.Interpretation;

namespace MyFinanceTracker.InputProcessing.Text.Structured;

internal sealed partial class StructuredTextInputProcessor(
    ITextCommandInterpreter interpreter,
    ITextCommandDispatcher dispatcher,
    ILogger<StructuredTextInputProcessor> logger) : ITextInputProcessor
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
                await MapExecutionResult(await dispatcher.Dispatch(identified.Command, ct), line),

            InterpretationResult.Unrecognized unrecognized =>
                new ActionResult.InvalidSyntax(
                    ErrorCode: unrecognized.ErrorCode,
                    Suggestion: unrecognized.Suggestion,
                    Examples: unrecognized.Examples
                ) { RawInput = line },

            _ => throw new UnreachableException($"Interpretation result {interpretation.GetType().Name} was not handled.")
        };
    }

    private static Task<ActionResult> MapExecutionResult(CommandExecutionResult executionResult, string rawInput)
    {
        ActionResult actionResult = executionResult switch
        {
            CommandExecutionResult.Transaction.Added added =>
                new ActionResult.Transaction.Added(added.Transactions) { RawInput = rawInput },

            CommandExecutionResult.Transaction.Deleted deleted =>
                new ActionResult.Transaction.Deleted(deleted.CategoryName, deleted.Date) { RawInput = rawInput },

            CommandExecutionResult.Category.Listed listed =>
                new ActionResult.Category.Listed(listed.Categories) { RawInput = rawInput },

            CommandExecutionResult.InvalidSyntax syntax =>
                new ActionResult.InvalidSyntax(
                    ErrorCode: syntax.ErrorCode,
                    Suggestion: syntax.Suggestion,
                    Examples: syntax.Examples
                ) { RawInput = rawInput },

            CommandExecutionResult.InvalidInput invalidInput =>
                new ActionResult.InvalidInput(invalidInput.Errors) { RawInput = rawInput },

            CommandExecutionResult.Failure failure =>
                new ActionResult.Failure(failure.Message) { RawInput = rawInput },

            _ => throw new UnreachableException($"Execution result {executionResult.GetType().Name} was not mapped.")
        };

        return Task.FromResult(actionResult);
    }
}