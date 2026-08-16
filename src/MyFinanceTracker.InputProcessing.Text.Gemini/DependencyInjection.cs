using Google.GenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MyFinanceTracker.InputProcessing.Text.Gemini.Configuration;
using MyFinanceTracker.InputProcessing.Text.Gemini.Declarations;
using MyFinanceTracker.InputProcessing.Text.Gemini.Execution;
using MyFinanceTracker.InputProcessing.Text.Gemini.Prompt;

namespace MyFinanceTracker.InputProcessing.Text.Gemini;

public static class DependencyInjection
{
    public static IProcessorConfigured UseGemini(
        this ITextInputProcessingBuilder builder,
        IConfiguration configuration)
    {
        builder.Services
            .ConfigureGeminiOptions(configuration)
            .AddScoped(sp =>
            {
                var options = sp.GetRequiredService<IOptions<GeminiOptions>>().Value;
                return new Client(apiKey: options.ApiKey);
            })
            .AddScoped<ITextInputProcessor, GeminiTextInputProcessor>()
            .AddScoped<IGeminiSystemPromptBuilder, GeminiSystemPromptBuilder>()
            .AddSingleton<IGeminiToolDeclarationProvider, GeminiToolDeclarationProvider>()
            .AddScoped<IGeminiToolExecutor, GeminiToolExecutor>();

        return new ProcessorConfigured(builder.Services);
    }

    private static IServiceCollection ConfigureGeminiOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddOptions<GeminiOptions>()
            .Bind(configuration.GetSection(GeminiOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart()
            .Services;
    }
}