using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Configuration;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Data;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Initialization;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Sync;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite;

public static class DependencyInjection
{
    public static IServiceCollection AddSqlitePersistence(this IServiceCollection services, IConfiguration configuration)
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

        services.AddSingleton<ITransactionRepository, SqliteTransactionRepository>();
        services.AddSingleton<OutboxProcessor>();
        services.AddHostedService<SqliteDatabaseInitializerHostedService>();
        services.AddHostedService<OutboxSyncBackgroundService>();

        return services;
    }
}
