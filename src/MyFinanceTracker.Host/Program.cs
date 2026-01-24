using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Host;
using MyFinanceTracker.Infrastructure.GoogleSheets;
using MyFinanceTracker.Infrastructure.Persistence.Yaml;
using MyFinanceTracker.Interactions;
using MyFinanceTracker.UseCases;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings 
{ 
    Args = args,
    ContentRootPath = AppContext.BaseDirectory 
});
builder.Services
    .AddYamlCategoryRepository(builder.Configuration)
    .AddInteractions()
    .AddUseCases()
    .AddGoogleSheetsPersistence(builder.Configuration)
    .AddConfiguredInteractions(builder.Configuration);
    
builder.Logging.AddConsole();

using var host = builder.Build();
await host.RunAsync();