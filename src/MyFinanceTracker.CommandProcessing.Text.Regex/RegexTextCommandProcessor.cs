using Microsoft.Extensions.Logging;
using MyFinanceTracker.CommandProcessing.Text.Regex.Interpretation;
using MyFinanceTracker.CommandProcessing.Text.Regex.Dispatching;
using System.Diagnostics;

namespace MyFinanceTracker.CommandProcessing.Text.Regex;

internal sealed partial class RegexTextCommandProcessor(
    ITextCommandInterpreter interpreter,
    ITextCommandDispatcher dispatcher,
    ILogger<RegexTextCommandProcessor> logger) : ITextCommandProcessor
{
    public async Task<TextCommandResponse> Execute(TextCommandRequest request, CancellationToken ct = default)
    {
        LogExecuteEntry(request);

        var interpretation = await interpreter.Interpret(request.Input);
        var response = interpretation switch
        {
            InterpretationResult.Identified identified =>
                await dispatcher.Dispatch(identified.Command, ct),

            InterpretationResult.Unrecognized unrecognized =>
                new TextCommandResponse.InvalidInput(
                    Details: $"Command '{unrecognized.Command}' is not recognized.",
                    Suggestion: unrecognized.Suggestion,
                    Examples: unrecognized.Examples),

            InterpretationResult.EmptyInput =>
                new TextCommandResponse.InvalidInput(
                    Details: "The provided text is empty or contains only whitespace."),

            _ => throw new UnreachableException($"Interpretation result {interpretation.GetType().Name} was not handled.")
        };

        LogExecuteExit(response);
        return response;
    }
}