using Google.GenAI;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MyFinanceTracker.InputProcessing.Gemini.Configuration;
using MyFinanceTracker.InputProcessing.Gemini.Declarations;
using MyFinanceTracker.InputProcessing.Gemini.Execution;
using MyFinanceTracker.InputProcessing.Gemini.Prompt;

namespace MyFinanceTracker.InputProcessing.Gemini;

public static class DependencyInjection
{
    public static IServiceCollection AddGeminiInputProcessor(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .ConfigureGeminiOptions(configuration)
            .AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<GeminiOptions>>().Value;
                return new Client(apiKey: options.ApiKey);
            })
            .AddScoped<IRequestHandler<Input.Text, ProcessingResult>, GeminiInputProcessor>()
            .AddScoped<IRequestHandler<Input.Audio, ProcessingResult>, GeminiInputProcessor>()
            .AddScoped<IRequestHandler<Input.Image, ProcessingResult>, GeminiInputProcessor>()
            .AddScoped<IGeminiSystemPromptBuilder, GeminiSystemPromptBuilder>()
            .AddSingleton<IGeminiToolDeclarationProvider, GeminiToolDeclarationProvider>()
            .AddScoped<IGeminiToolExecutor, GeminiToolExecutor>();

        return services;
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
