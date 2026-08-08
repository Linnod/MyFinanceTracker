using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.CommandProcessing.Text;
using MyFinanceTracker.CommandProcessing.Text.Gemini;
using MyFinanceTracker.Host;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets;
using MyFinanceTracker.Infrastructure.Persistence.Yaml;
using MyFinanceTracker.UseCases;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .AddYamlCategoryRepository(builder.Configuration)
    .AddTextCommandProcessing(b => b.UseGemini(builder.Configuration))
    .AddUseCases()
    .AddGoogleSheetsPersistence(builder.Configuration)
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