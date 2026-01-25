using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Google.Apis.Sheets.v4;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.Infrastructure.GoogleSheets.Repositories;
using MyFinanceTracker.Infrastructure.GoogleSheets.Configuration;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using MyFinanceTracker.Infrastructure.GoogleSheets.Clients;
using MyFinanceTracker.Infrastructure.GoogleSheets.Mapping;
using MyFinanceTracker.Infrastructure.GoogleSheets.Services;
using System.Text.Json;

namespace MyFinanceTracker.Infrastructure.GoogleSheets;

public static class DependencyInjection
{
    public static IServiceCollection AddGoogleSheetsPersistence(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.ConfigureGoogleSheetsOptions(configuration)
            .AddSheetsService(configuration)
            .AddSingleton<ITransactionRepository, GoogleSheetsTransactionRepository>()
            .AddSingleton<GoogleSheetMapper>()
            .AddSingleton<IGoogleSheetsClient, GoogleSheetsClient>()
            .AddFormulaServices();

        return services;
    }

    private static IServiceCollection AddFormulaServices(this IServiceCollection services)
    {
        return services.AddSingleton<FormulaService>()
            .AddSingleton<FormulaBuilder>();
    }

    private static IServiceCollection AddSheetsService(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddSingleton<SheetsService>(sp =>
            {
                var opt = sp.GetRequiredService<IOptions<GoogleSheetsOptions>>().Value;
                var credential = GoogleCredential.FromFile(opt.CredentialsFilePath)
                    .CreateScoped(SheetsService.Scope.Spreadsheets);

                return new SheetsService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = opt.ApplicationName
                });
            });
    }

    private static IServiceCollection ConfigureGoogleSheetsOptions(this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddOptions<GoogleSheetsOptions>()
                .Bind(configuration.GetSection(GoogleSheetsOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart()
                .Services;
    }
}