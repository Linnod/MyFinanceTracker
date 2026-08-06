using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using MyFinanceTracker.Domain.Entities;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MyFinanceTracker.Interactions.Api.Extensions;

internal static class ApiSwaggerExtensions
{
    public static IServiceCollection AddApiSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        return services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new()
            {
                Title = "MyFinanceTracker REST API for ChatGPT",
                Version = "v1",
                Description = "API for managing personal finances via structured REST endpoints."
            });

            options.MapType<TransactionType>(() => new OpenApiSchema
            {
                Type = typeof(string).Name.ToLowerInvariant(),
                Enum = [.. Enum.GetNames<TransactionType>().Select(name => new OpenApiString(name))]
            });

            options.OperationFilter<NgrokSkipWarningFilter>();

            options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Description = "API Key authentication using X-Api-Key header",
                Type = SecuritySchemeType.ApiKey,
                Name = "X-Api-Key",
                In = ParameterLocation.Header,
                Scheme = "ApiKeyScheme"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "ApiKey"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
    }

    public static WebApplication UseApiSwagger(this WebApplication app)
    {
        app.UseSwagger(options =>
        {
            options.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
            {
                var scheme = httpReq.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? httpReq.Scheme;
                var host = httpReq.Headers["X-Forwarded-Host"].FirstOrDefault() ?? httpReq.Host.Value;
                swaggerDoc.Servers = [new OpenApiServer { Url = $"{scheme}://{host}" }];
            });
        });

        app.UseSwaggerUI();
        return app;
    }

    private sealed class NgrokSkipWarningFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            operation.Parameters ??= [];
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "ngrok-skip-browser-warning",
                In = ParameterLocation.Query,
                Required = false,
                Schema = new OpenApiSchema { Type = "string", Default = new OpenApiString("true") },
                Description = "Skip Ngrok warning page"
            });
        }
    }
}