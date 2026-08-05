using MyFinanceTracker.CommandProcessing.Text;
using MyFinanceTracker.Interactions.Api.Extensions;

namespace MyFinanceTracker.Interactions.Api;

internal static class ApiServerBuilder
{
    public static WebApplication Build(ApiInteractionOptions options, ITextCommandReceiver commandReceiver)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://*:{options.Port}");
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        builder.Services.AddApiSwagger();
        builder.Services.AddSingleton(commandReceiver);

        var app = builder.Build();

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
                        Status = "Unauthorized",
                        Error = "Invalid or missing API Key."
                    });
                    return;
                }
            }

            await next();
        });

        app.UseApiSwagger();
        app.MapApiEndpoints();

        return app;
    }
}