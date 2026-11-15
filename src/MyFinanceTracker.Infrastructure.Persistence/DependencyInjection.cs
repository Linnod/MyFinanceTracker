using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Clients;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Configuration;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Mapping;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Data;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Initialization;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Options;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Sync;

namespace MyFinanceTracker.Infrastructure.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddGoogleSheetsPersistence(configuration)
            .AddSqlitePersistence(configuration);
    }

    private static IServiceCollection AddGoogleSheetsPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureGoogleSheetsOptions(configuration)
            .AddSheetsService()
            .AddSingleton<IRemoteCategoryRepository, GoogleSheetsCategoryRepository>()
            .AddSingleton<IRemoteTransactionRepository, GoogleSheetsTransactionRepository>()
            .AddSingleton<GoogleSheetTransactionMapper>()
            .AddSingleton<GoogleSheetCategoryMapper>()
            .AddSingleton<IGoogleSheetsClient, GoogleSheetsClient>();

        return services;
    }

    private static IServiceCollection AddSqlitePersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SqlitePersistenceOptions>()
            .Bind(configuration.GetSection(SqlitePersistenceOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddDbContextFactory<SqliteDbContext>((sp, builder) =>
        {
            var opt = sp.GetRequiredService<IOptions<SqlitePersistenceOptions>>().Value;
            builder.UseSqlite(opt.ConnectionString);
        });

        services.AddSingleton<ICategoryRepository, SqliteCategoryRepository>();
        services.AddSingleton<ITransactionRepository, SqliteTransactionRepository>();
        services.AddSingleton<IOutboxNotificationPublisher, OutboxNotificationPublisher>();
        services.AddSingleton<OutboxProcessor>();
        services.AddHostedService<SqliteDatabaseInitializerHostedService>();
        services.AddHostedService<OutboxSyncBackgroundService>();

        return services;
    }

    private static IServiceCollection AddSheetsService(this IServiceCollection services)
    {
        return services.AddSingleton(sp =>
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

    private static IServiceCollection ConfigureGoogleSheetsOptions(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddOptions<GoogleSheetsOptions>()
            .Bind(configuration.GetSection(GoogleSheetsOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(opt => File.Exists(opt.CredentialsFilePath), "Google Service Account key file is missing at the specified path.")
            .ValidateOnStart()
            .Services;
    }
}
