using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Infrastructure.GoogleSheets;
using MyFinanceTracker.Infrastructure.Persistence.Yaml;
using MyFinanceTracker.Interactions;
using MyFinanceTracker.Interactions.Console;
using MyFinanceTracker.UseCases;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .AddYamlCategoryRepository(builder.Configuration)
    .AddConsoleInteractions()
    .AddInteractions()
    .AddUseCases()
    .AddGoogleSheetsPersistence(builder.Configuration);
    
builder.Logging.AddConsole();

using var host = builder.Build();
await host.RunAsync();