using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Host;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite;
using MyFinanceTracker.Infrastructure.Persistence.Yaml;
using MyFinanceTracker.UseCases;
using MyFinanceTracker.InputProcessing.Gemini;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .AddSingleton(TimeProvider.System)
    .AddYamlCategoryRepository(builder.Configuration)
    .AddGeminiInputProcessor(builder.Configuration)
    .AddUseCases()
    .AddGoogleSheetsPersistence(builder.Configuration)
    .AddSqlitePersistence(builder.Configuration)
    .AddConfiguredInteractions(builder.Configuration);

builder.Logging
    .AddSimpleConsole(options =>
    {
        options.IncludeScopes = true;
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    })
    .AddFilter("Microsoft", LogLevel.Warning)
    .AddFilter("System", LogLevel.Warning)
    .AddFilter("MyFinanceTracker", LogLevel.Information);
using var host = builder.Build();
await host.RunAsync();