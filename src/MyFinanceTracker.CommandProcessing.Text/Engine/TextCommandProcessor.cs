using Microsoft.Extensions.Logging;
using MyFinanceTracker.CommandProcessing.Text.Engine.Interpretation;
using MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching;
using System.Diagnostics;

namespace MyFinanceTracker.CommandProcessing.Text.Engine;

internal sealed partial class TextCommandProcessor(
    ITextCommandInterpreter interpreter,
    ITextCommandDispatcher dispatcher,
    ILogger<TextCommandProcessor> logger) : ITextCommandProcessor
{
    public async Task<TextCommandResponse> Execute(TextCommandRequest request, CancellationToken ct = default)
    {
        LogExecuteEntry(request);

        var interpretation = await interpreter.Interpret(request.Input);
        var response = interpretation switch
        {
            InterpretationResult.Identified identified =>
                await dispatcher.Dispatch(identified.Type, identified.Payload, ct),

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