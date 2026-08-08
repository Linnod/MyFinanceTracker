using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyFinanceTracker.CommandProcessing.Text.Gemini.Configuration;
using MyFinanceTracker.CommandProcessing.Text.Gemini.Declarations;
using MyFinanceTracker.CommandProcessing.Text.Gemini.Execution;
using MyFinanceTracker.CommandProcessing.Text.Gemini.Prompt;

namespace MyFinanceTracker.CommandProcessing.Text.Gemini;

internal sealed partial class GeminiTextCommandProcessor(
    IGeminiSystemPromptBuilder promptBuilder,
    IGeminiToolDeclarationProvider declarationProvider,
    IGeminiToolExecutor toolExecutor,
    IOptions<GeminiOptions> options,
    Client client,
    ILogger<GeminiTextCommandProcessor> logger) : ITextCommandProcessor
{
    private readonly GeminiOptions _options = options.Value;

    public async Task<TextCommandResponse> Execute(TextCommandRequest request, CancellationToken ct = default)
    {
        LogExecuteEntry(request);

        try
        {
            var config = new GenerateContentConfig
            {
                SystemInstruction = new Content
                {
                    Parts = [new Part { Text = await promptBuilder.BuildSystemInstructionAsync(ct) }]
                },
                Tools = [.. declarationProvider.GetToolDeclarations()],
                Temperature = _options.Temperature
            };

            var response = await client.Models.GenerateContentAsync(
                model: _options.Model,
                contents: request.Input,
                config: config,
                ct
            );

            var functionCalls = response.FunctionCalls;
            if (functionCalls != null && functionCalls.Count > 0)
            {
                var result = await ExecuteTools(functionCalls, ct);

                LogExecuteExit(result);
                return result;
            }

            var textResponse = response.Text;
            if (!string.IsNullOrWhiteSpace(textResponse))
            {
                LogTextResponse();
                return new TextCommandResponse.InvalidInput(textResponse);
            }

            LogEmptyResponse();
            return new TextCommandResponse.InvalidInput("Gemini did not produce any output or tool call.");
        }
        catch (Exception ex)
        {
            LogError(ex);
            return new TextCommandResponse.SystemError("AI processing failed. Try again later.");
        }
    }

    private async Task<TextCommandResponse> ExecuteTools(List<FunctionCall> functionCalls, CancellationToken ct)
    {
        var results = new List<TextCommandResponse>();
        foreach (var call in functionCalls)
        {
            results.Add(await toolExecutor.ExecuteToolCallAsync(call, ct));
        }
        
        return CombineResponses(results);
    }

    private static TextCommandResponse CombineResponses(List<TextCommandResponse> results)
    {
        var systemError = results.OfType<TextCommandResponse.SystemError>().FirstOrDefault();
        if (systemError != null)
        {
            return systemError;
        }

        var successes = results.OfType<TextCommandResponse.Success>().ToList();
        var invalidInputs = results.OfType<TextCommandResponse.InvalidInput>().ToList();
        var logicErrors = results.OfType<TextCommandResponse.LogicError>().ToList();

        if (successes.Count > 0)
        {
            var combinedDetails = new List<TextCommandResponseDetail>();

            foreach (var s in successes)
            {
                combinedDetails.Add(FormatSuccessItemAsDetail(s));
            }

            foreach (var invalid in invalidInputs)
            {
                var errorMsg = !string.IsNullOrWhiteSpace(invalid.Suggestion)
                    ? $"{invalid.Details} (Suggestion: {invalid.Suggestion})"
                    : invalid.Details;

                combinedDetails.Add(new TextCommandResponseDetail(
                    Name: "Failed action",
                    Value: errorMsg ?? "Invalid input parameters",
                    Icon: "⚠️"
                ));
            }

            foreach (var logicErr in logicErrors)
            {
                combinedDetails.Add(new TextCommandResponseDetail(
                    Name: "Failed action",
                    Value: logicErr.Message ?? "Domain logic error",
                    Icon: "❌"
                ));
            }

            var total = results.Count;
            var failedCount = invalidInputs.Count + logicErrors.Count;

            var primaryValue = failedCount == 0
                ? $"Successfully processed {successes.Count} operations"
                : $"Processed {successes.Count} of {total} operations ({failedCount} failed)";

            return new TextCommandResponse.Success(
                CommandDescription: $"Executed {total} actions via Gemini AI",
                PrimaryValue: primaryValue,
                Details: combinedDetails
            );
        }

        if (invalidInputs.Count > 0)
        {
            var messages = string.Join("\n", invalidInputs.Select(i => $"• {i.Details}"));
            return new TextCommandResponse.InvalidInput(messages);
        }

        if (logicErrors.Count > 0)
        {
            var messages = string.Join("\n", logicErrors.Select(l => $"• {l.Message}"));
            return new TextCommandResponse.LogicError(messages);
        }

        return new TextCommandResponse.LogicError("Failed to process combined tool calls.");
    }

    private static TextCommandResponseDetail FormatSuccessItemAsDetail(TextCommandResponse.Success item)
    {
        var innerParams = item.Details.Select(d => $"{d.Name}: {d.Value}");
        var paramsSummary = string.Join(" | ", innerParams);

        var valueText = string.IsNullOrWhiteSpace(paramsSummary)
            ? "Completed"
            : paramsSummary;

        return new TextCommandResponseDetail(
            Name: item.PrimaryValue,
            Value: valueText,
            Icon: "🔹"
        );
    }
}