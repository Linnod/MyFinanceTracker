using Google.GenAI;
using Google.GenAI.Types;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyFinanceTracker.InputProcessing.Gemini.Configuration;
using MyFinanceTracker.InputProcessing.Gemini.Declarations;
using MyFinanceTracker.InputProcessing.Gemini.Execution;
using MyFinanceTracker.InputProcessing.Gemini.Prompt;

using MyFinanceTracker.UseCases;

namespace MyFinanceTracker.InputProcessing.Gemini;

internal sealed partial class GeminiInputProcessor(
    IGeminiSystemPromptBuilder promptBuilder,
    IGeminiToolDeclarationProvider declarationProvider,
    IGeminiToolExecutor toolExecutor,
    IOptions<GeminiOptions> options,
    Client client,
    ILogger<GeminiInputProcessor> logger)
    : IRequestHandler<Input.Text, ProcessingResult>,
      IRequestHandler<Input.Audio, ProcessingResult>,
      IRequestHandler<Input.Image, ProcessingResult>
{
    private const string ProcessingFailureMessage = "Failed to process your request. Please try again later.";
    private const string ExtractionInstruction = "Extract financial transaction details and execute the corresponding function call.";
    private const string AudioInstruction = "Process the user's voice message and execute the corresponding function call.";

    public Task<ProcessingResult> Handle(Input.Text request, CancellationToken ct)
    {
        LogExecuteEntry(request);

        var content = CreateContent(request.Context, new Part { Text = request.Content });

        return ProcessCore(content, ct);
    }

    public Task<ProcessingResult> Handle(Input.Audio request, CancellationToken ct)
    {
        LogExecuteEntry(request);

        return ProcessCore(CreateAudioContent(request), ct);
    }

    public Task<ProcessingResult> Handle(Input.Image request, CancellationToken ct)
    {
        LogExecuteEntry(request);

        return ProcessCore(CreateImageContent(request), ct);
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
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogError(ex);
            return new ProcessingResult.SystemError(ProcessingFailureMessage);
        }
    }

    private static Content CreateImageContent(Input.Image input) =>
        CreateMediaContent(input.Data, input.ContentType, ExtractionInstruction, input.Caption, input.Context);

    private static Content CreateAudioContent(Input.Audio input) =>
        CreateMediaContent(input.Data, input.ContentType, AudioInstruction, input.Caption, input.Context);

    private static Content CreateMediaContent(
        byte[] data,
        string contentType,
        string instruction,
        string? caption,
        string? context)
    {
        var promptText = string.IsNullOrWhiteSpace(caption)
            ? instruction
            : $"{instruction} User caption: {caption}";

        return CreateContent(
            context,
            Part.FromBytes(data, contentType),
            new Part { Text = promptText });
    }

    private static Content CreateContent(string? context, params Part[] primaryParts)
    {
        List<Part> parts = string.IsNullOrWhiteSpace(context)
            ? [.. primaryParts]
            : [.. primaryParts, new Part { Text = FormatContext(context) }];

        return new Content
        {
            Role = "user",
            Parts = parts
        };
    }

    private static string FormatContext(string context) =>
        $"[Context - reference only, do not re-execute]\n{context}\n[End of context]";

    private async Task<GenerateContentConfig> BuildConfig(CancellationToken ct)
        => new()
        {
            Temperature = 0f,
            SystemInstruction = new Content
            {
                Parts = [new Part { Text = await promptBuilder.BuildSystemInstruction(ct) }]
            },
            Tools = [.. declarationProvider.GetToolDeclarations()]
        };

    private async Task<List<UseCaseResponse>> ExecuteTools(List<FunctionCall> functionCalls, CancellationToken ct)
    {
        var results = new List<UseCaseResponse>();
        foreach (var call in functionCalls)
        {
            results.Add(await toolExecutor.ExecuteToolCall(call, ct));
        }
        return results;
    }
}
