using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Common.Notifications;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Configuration;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Data;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Sync;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

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
        // arrange
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
            Substitute.For<IOutboxNotificationPublisher>(),
            processorLogger);

        var service = new OutboxSyncBackgroundService(processor, optionsWrapper, Substitute.For<IOutboxNotificationPublisher>(), workerLogger);

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // act
        var act = () => service.StartAsync(cts.Token);

        // assert
        await act.Should().NotThrowAsync();

        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_OutboxProcessorThrowsException_PublishesSyncFailedNotification()
    {
        // arrange
        var brokenContextFactory = Substitute.For<IDbContextFactory<SqliteDbContext>>();
        brokenContextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Broken DB context factory"));

        var optionsWrapper = Microsoft.Extensions.Options.Options.Create(new SqlitePersistenceOptions
        {
            SyncIntervalSeconds = 1
        });

        var notificationPublisher = Substitute.For<IOutboxNotificationPublisher>();
        var notificationPublished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        notificationPublisher.Publish(
            Arg.Any<OutboxSyncNotification.SyncFailed>(),
            Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                notificationPublished.TrySetResult();
                return Task.CompletedTask;
            });

        using var processor = new OutboxProcessor(
            brokenContextFactory,
            remoteRepository,
            categoryRepository,
            TimeProvider.System,
            optionsWrapper,
            notificationPublisher,
            processorLogger);

        using var service = new OutboxSyncBackgroundService(processor, optionsWrapper, notificationPublisher, workerLogger);

        // act
        await service.StartAsync(CancellationToken.None);

        var completed = await Task.WhenAny(notificationPublished.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        await service.StopAsync(CancellationToken.None);

        // assert
        completed.Should().Be(notificationPublished.Task);
        await notificationPublisher.Received(1).Publish(
            Arg.Is<OutboxSyncNotification.SyncFailed>(n => n.ErrorMessage == "Broken DB context factory"),
            Arg.Any<CancellationToken>());
    }

    private sealed class TestDbContextFactory(DbContextOptions<SqliteDbContext> options) : IDbContextFactory<SqliteDbContext>
    {
        public SqliteDbContext CreateDbContext() => new(options);

        public Task<SqliteDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new SqliteDbContext(options));
    }
}
