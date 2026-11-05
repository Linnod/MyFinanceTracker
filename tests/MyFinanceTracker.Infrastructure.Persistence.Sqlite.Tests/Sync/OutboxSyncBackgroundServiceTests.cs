using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Configuration;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Data;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Sync;
using NSubstitute;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Tests.Sync;

public sealed class OutboxSyncBackgroundServiceTests : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly DbContextOptions<SqliteDbContext> options;
    private readonly IDbContextFactory<SqliteDbContext> contextFactory;
    private readonly ITransactionRepository remoteRepository;
    private readonly ICategoryRepository categoryRepository;
    private readonly ILogger<OutboxProcessor> processorLogger;
    private readonly ILogger<OutboxSyncBackgroundService> workerLogger;

    public OutboxSyncBackgroundServiceTests()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        options = new DbContextOptionsBuilder<SqliteDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new SqliteDbContext(options);
        context.Database.EnsureCreated();

        contextFactory = new TestDbContextFactory(options);
        remoteRepository = Substitute.For<ITransactionRepository>();
        categoryRepository = Substitute.For<ICategoryRepository>();
        processorLogger = Substitute.For<ILogger<OutboxProcessor>>();
        workerLogger = Substitute.For<ILogger<OutboxSyncBackgroundService>>();
    }

    public void Dispose()
    {
        connection.Dispose();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCancelCleanly()
    {
        // Arrange
        var optionsWrapper = Microsoft.Extensions.Options.Options.Create(new SqlitePersistenceOptions
        {
            SyncIntervalSeconds = 5
        });

        var processor = new OutboxProcessor(
            contextFactory,
            remoteRepository,
            categoryRepository,
            TimeProvider.System,
            optionsWrapper,
            processorLogger);

        var service = new OutboxSyncBackgroundService(processor, optionsWrapper, workerLogger);

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        var act = () => service.StartAsync(cts.Token);
        await act.Should().NotThrowAsync();

        await service.StopAsync(CancellationToken.None);
    }

    private sealed class TestDbContextFactory(DbContextOptions<SqliteDbContext> options) : IDbContextFactory<SqliteDbContext>
    {
        public SqliteDbContext CreateDbContext() => new(options);

        public Task<SqliteDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new SqliteDbContext(options));
    }
}
