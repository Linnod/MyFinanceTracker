using Google.GenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MyFinanceTracker.CommandProcessing.Text.Gemini.Configuration;
using MyFinanceTracker.CommandProcessing.Text.Gemini.Declarations;
using MyFinanceTracker.CommandProcessing.Text.Gemini.Execution;
using MyFinanceTracker.CommandProcessing.Text.Gemini.Prompt;

namespace MyFinanceTracker.CommandProcessing.Text.Gemini;

public static class DependencyInjection
{
    public static IProcessorConfigured UseGemini(
        this ITextCommandProcessingBuilder builder,
        IConfiguration configuration)
    {
        builder.Services
            .ConfigureGeminiOptions(configuration)
            .AddScoped(sp => 
            {
                var options = sp.GetRequiredService<IOptions<GeminiOptions>>().Value;
                return new Client(apiKey: options.ApiKey);
            })
            .AddScoped<ITextCommandProcessor, GeminiTextCommandProcessor>()
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