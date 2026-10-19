using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyFinanceTracker.InputProcessing.Inputs;
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
    : IInputProcessor<TextInput>, IInputProcessor<AudioInput>
{
    private const string ProcessingFailureMessage = "Failed to process your request. Please try again later.";

    public Task<ProcessingResult> Process(TextInput input, CancellationToken ct)
    {
        LogExecuteEntry(input);

        var content = new Content
        {
            Role = "user",
            Parts = [new Part { Text = input.Text }]
        };

        return ProcessCore(Task.FromResult(content), ct);
    }

    public Task<ProcessingResult> Process(AudioInput input, CancellationToken ct)
    {
        LogExecuteEntry(input);

        return ProcessCore(CreateAudioContent(input, ct), ct);
    }

    private async Task<ProcessingResult> ProcessCore(Task<Content> contentTask, CancellationToken ct)
    {
        try
        {
            var content = await contentTask;
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
            return new ProcessingResult.SystemError(ProcessingFailureMessage, ex);
        }
    }

    private static async Task<Content> CreateAudioContent(AudioInput input, CancellationToken ct)
    {
        var audioBytes = await ReadBytesAsync(input.Stream, ct);
        return new Content
        {
            Parts =
            [
                Part.FromBytes(audioBytes, input.ContentType),
                new Part { Text = "Extract financial transaction details and execute the corresponding function call." }
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

    private static async Task<byte[]> ReadBytesAsync(Stream stream, CancellationToken ct)
    {
        if (stream is MemoryStream ms)
        {
            return ms.ToArray();
        }

        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, ct);
        return memoryStream.ToArray();
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
