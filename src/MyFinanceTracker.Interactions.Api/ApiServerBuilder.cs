using System.Text.Json.Serialization;
using MyFinanceTracker.Interactions.Api.Extensions;

namespace MyFinanceTracker.Interactions.Api;

internal static class ApiServerBuilder
{
    public static WebApplication Build(ApiInteractionOptions options, IServiceProvider rootServiceProvider)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://*:{options.Port}");
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        builder.Services.ConfigureHttpJsonOptions(opt =>
        {
            opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        builder.Services.AddApiSwagger();

        var app = builder.Build();

        app.UseApiSwagger();

        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                if (!context.Request.Headers.TryGetValue("X-Api-Key", out var extractedKey) ||
                    !string.Equals(extractedKey, options.ApiKey, StringComparison.Ordinal))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        Error = "Invalid or missing API Key."
                    });
                    return;
                }

                using var scope = rootServiceProvider.CreateScope();
                context.RequestServices = scope.ServiceProvider;

                await next();
                return;
            }

            await next();
        });

        app.MapApiEndpoints();

        return app;
    }
}