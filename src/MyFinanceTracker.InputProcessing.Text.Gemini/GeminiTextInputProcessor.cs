using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyFinanceTracker.InputProcessing.Text.Gemini.Configuration;
using MyFinanceTracker.InputProcessing.Text.Gemini.Declarations;
using MyFinanceTracker.InputProcessing.Text.Gemini.Execution;
using MyFinanceTracker.InputProcessing.Text.Gemini.Prompt;

namespace MyFinanceTracker.InputProcessing.Text.Gemini;

internal sealed partial class GeminiTextInputProcessor(
    IGeminiSystemPromptBuilder promptBuilder,
    IGeminiToolDeclarationProvider declarationProvider,
    IGeminiToolExecutor toolExecutor,
    IOptions<GeminiOptions> options,
    Client client,
    ILogger<GeminiTextInputProcessor> logger) : ITextInputProcessor
{
    private readonly GeminiOptions _options = options.Value;

    public async Task<ProcessingResult> Process(TextInput input, CancellationToken ct)
    {
        LogExecuteEntry(input);

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
                contents: input.Text,
                config: config,
                ct
            );

            var functionCalls = response.FunctionCalls;
            if (functionCalls != null && functionCalls.Count > 0)
            {
                var results = await ExecuteTools(functionCalls, ct);
                var result = new ProcessingResult.Completed(Actions: results);

                LogExecuteExit(result);
                return result;
            }

            var textResponse = response.Text;
            if (!string.IsNullOrWhiteSpace(textResponse))
            {
                LogTextResponse();
                return new ProcessingResult.InvalidInput(textResponse);
            }

            LogEmptyResponse();
            return new ProcessingResult.InvalidInput("Gemini did not produce any output or tool call.");
        }
        catch (Exception ex)
        {
            LogError(ex);
            return new ProcessingResult.SystemError("AI processing failed. Try again later.", ex);
        }
    }

    private async Task<List<ActionResult>> ExecuteTools(List<FunctionCall> functionCalls, CancellationToken ct)
    {
        var results = new List<ActionResult>();
        foreach (var call in functionCalls)
        {
            results.Add(await toolExecutor.ExecuteToolCall(call, ct));
        }
        return results;
    }
}