using System.ComponentModel.DataAnnotations;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Configuration;

internal sealed class SqlitePersistenceOptions
{
    public const string SectionName = "SqlitePersistence";

    [Required(AllowEmptyStrings = false)]
    public string ConnectionString { get; init; } = "Data Source=finance.db";

    [Range(5, 3600)]
    public int SyncIntervalSeconds { get; init; } = 60;

    [Range(1, 50)]
    public int MaxRetryAttempts { get; init; } = 5;
}
