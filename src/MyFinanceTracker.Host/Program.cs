using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Host.Configuration;
using MyFinanceTracker.Interactions.Telegram;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite;
using MyFinanceTracker.Infrastructure.Persistence.Yaml;
using MyFinanceTracker.UseCases;
using MyFinanceTracker.InputProcessing.Gemini;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddSingleton(TimeProvider.System)
    .AddOptionalApplicationInsights(builder.Configuration)
    .AddYamlCategoryRepository(builder.Configuration)
    .AddGeminiInputProcessor(builder.Configuration)
    .AddUseCases()
    .AddGoogleSheetsPersistence(builder.Configuration)
    .AddSqlitePersistence(builder.Configuration)
    .AddTelegramInteractions(builder.Configuration);

builder.Logging
    .AddSimpleConsole(options =>
    {
        options.IncludeScopes = true;
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    });

using var host = builder.Build();
await host.RunAsync();