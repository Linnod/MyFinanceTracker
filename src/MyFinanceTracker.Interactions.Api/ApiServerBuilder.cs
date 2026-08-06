using System.Text.Json.Serialization;
using MyFinanceTracker.Interactions.Api.Extensions;

namespace MyFinanceTracker.Interactions.Api;

internal static partial class ApiServerBuilder
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
                using var scope = rootServiceProvider.CreateScope();
                context.RequestServices = scope.ServiceProvider;

                var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("MyFinanceTracker.Interactions.Api");

                LogRequestReceived(logger, context.Request.Method, context.Request.Path);

                if (!context.Request.Headers.TryGetValue("X-Api-Key", out var extractedKey) ||
                    !string.Equals(extractedKey, options.ApiKey, StringComparison.Ordinal))
                {
                    LogUnauthorized(logger, context.Request.Path);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { Error = "Invalid or missing API Key." });
                    return;
                }

                await next();

                LogRequestFinished(logger, context.Request.Method, context.Request.Path, context.Response.StatusCode);
                return;
            }

            await next();
        });

        app.MapApiEndpoints();

        return app;
    }
}