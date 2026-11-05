using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyFinanceTracker.InputProcessing.Text.Gemini.Configuration;
using MyFinanceTracker.InputProcessing.Text.Gemini.Declarations;
using MyFinanceTracker.InputProcessing.Text.Gemini.Execution;
using MyFinanceTracker.InputProcessing.Text.Gemini.Prompt;

namespace MyFinanceTracker.InputProcessing.Text.Gemini;

internal sealed partial class GeminiInputProcessor(
    IGeminiSystemPromptBuilder promptBuilder,
    IGeminiToolDeclarationProvider declarationProvider,
    IGeminiToolExecutor toolExecutor,
    IOptions<GeminiOptions> options,
    Client client,
    ILogger<GeminiInputProcessor> logger)
    : IInputProcessor<Input.Text>, IInputProcessor<Input.Audio>, IInputProcessor<Input.Image>
{
    private const string ProcessingFailureMessage = "Failed to process your request. Please try again later.";
    private const string ExtractionInstruction = "Extract financial transaction details and execute the corresponding function call.";

    public Task<ProcessingResult> Process(Input.Text input, CancellationToken ct)
    {
        LogExecuteEntry(input);

        var content = new Content
        {
            Role = "user",
            Parts = [new Part { Text = input.Content }]
        };

        return ProcessCore(content, ct);
    }

    public Task<ProcessingResult> Process(Input.Audio input, CancellationToken ct)
    {
        LogExecuteEntry(input);

        return ProcessCore(CreateAudioContent(input), ct);
    }

    public Task<ProcessingResult> Process(Input.Image input, CancellationToken ct)
    {
        LogExecuteEntry(input);

        return ProcessCore(CreateImageContent(input), ct);
    }

    private async Task<ProcessingResult> ProcessCore(Content content, CancellationToken ct)
    {
        try
        {
            var config = await BuildConfig(ct);
            var response = await client.Models.GenerateContentAsync(
                model: options.Value.Model,
                contents: [content],
                config: config,
                cancellationToken: ct);

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
            return new ProcessingResult.SystemError(ProcessingFailureMessage);
        }
        catch (Exception ex)
        {
            LogError(ex);
            return new ProcessingResult.SystemError(ProcessingFailureMessage);
        }
    }

    private static Content CreateImageContent(Input.Image input)
    {
        var promptText = string.IsNullOrWhiteSpace(input.Caption)
            ? ExtractionInstruction
            : $"{ExtractionInstruction} User caption: {input.Caption}";

        return new Content
        {
            Parts =
            [
                Part.FromBytes(input.Data, input.ContentType),
                new Part { Text = promptText }
            ]
        };
    }

    private static Content CreateAudioContent(Input.Audio input)
    {
        return new Content
        {
            Parts =
            [
                Part.FromBytes(input.Data, input.ContentType),
                new Part { Text = ExtractionInstruction }
            ]
        };
    }

    private async Task<GenerateContentConfig> BuildConfig(CancellationToken ct)
        => new()
        {
            SystemInstruction = new Content
            {
                Parts = [new Part { Text = await promptBuilder.BuildSystemInstruction(ct) }]
            },
            Tools = [.. declarationProvider.GetToolDeclarations()]
        };

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
