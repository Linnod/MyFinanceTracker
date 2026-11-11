using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Domain.Common;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Options;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Data;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Entities;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Initialization;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Models;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Sync;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Tests.Initialization;

public sealed class SqliteDatabaseInitializerHostedServiceTests : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly DbContextOptions<SqliteDbContext> options;
    private readonly IDbContextFactory<SqliteDbContext> contextFactory;
    private readonly ITransactionRepository transactionRepository;
    private readonly ICategoryRepository categoryRepository;
    private readonly ILogger<OutboxProcessor> outboxLogger;
    private readonly ILogger<SqliteDatabaseInitializerHostedService> initializerLogger;

    private readonly Category foodCategory = new("food", "Food");
    private readonly Category salaryCategory = new("salary", "Salary");

    public SqliteDatabaseInitializerHostedServiceTests()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        options = new DbContextOptionsBuilder<SqliteDbContext>()
            .UseSqlite(connection)
            .Options;

        // Create tables
        using var context = new SqliteDbContext(options);
        context.Database.EnsureCreated();

        contextFactory = new TestDbContextFactory(options);
        transactionRepository = Substitute.For<ITransactionRepository>();
        categoryRepository = Substitute.For<ICategoryRepository>();
        outboxLogger = Substitute.For<ILogger<OutboxProcessor>>();
        initializerLogger = Substitute.For<ILogger<SqliteDatabaseInitializerHostedService>>();

        categoryRepository.GetById(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var query = callInfo.Arg<string>();
                return new[] { foodCategory, salaryCategory }
                    .FirstOrDefault(c => c.Id.Equals(query, StringComparison.OrdinalIgnoreCase));
            });
    }

    public void Dispose()
    {
        connection.Dispose();
    }

    private OutboxProcessor CreateOutboxProcessor() =>
        new(contextFactory,
            transactionRepository,
            categoryRepository,
            TimeProvider.System,
            Microsoft.Extensions.Options.Options.Create(new SqlitePersistenceOptions()),
            Substitute.For<IOutboxNotificationPublisher>(),
            outboxLogger);

    private SqliteDatabaseInitializerHostedService CreateInitializer(OutboxProcessor? processor = null) =>
        new(contextFactory, transactionRepository, processor ?? CreateOutboxProcessor(), initializerLogger);

    [Fact]
    public async Task StartAsync_ShouldFlushOutboxFirst_ThenOverwriteTransactionsFromRemote()
    {
        // Arrange
        var oldLocalTxId = Guid.NewGuid();
        var pendingOutboxTxId = Guid.NewGuid();
        var remoteTxId = Guid.NewGuid();

        // 1. Seed existing local transaction that should be wiped
        // 2. Seed pending outbox message that should be flushed first
        await using (var seedContext = new SqliteDbContext(options))
        {
            seedContext.Transactions.Add(new TransactionEntity
            {
                Id = oldLocalTxId,
                Type = TransactionType.Expense,
                CategoryId = "food",
                Amount = 99m,
                Date = new DateTime(2026, 10, 1, 10, 0, 0),
                Note = "Stale local transaction"
            });

            var pendingOutboxDto = new TransactionOutboxDto(
                pendingOutboxTxId,
                TransactionType.Expense,
                "food",
                45m,
                new DateTime(2026, 11, 1, 10, 0, 0),
                "Pending outbox item");

            seedContext.OutboxMessages.Add(new OutboxMessageEntity
            {
                OperationType = OutboxOperationType.Add,
                PayloadJson = JsonSerializer.Serialize(new AddTransactionsOutboxPayload([pendingOutboxDto])),
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            await seedContext.SaveChangesAsync();
        }

        // Remote repository returns a fresh list of transactions
        var remoteTransaction = new Transaction(
            remoteTxId,
            TransactionType.Income,
            salaryCategory,
            5000m,
            new DateTime(2026, 11, 5, 10, 0, 0),
            "Monthly salary");

        transactionRepository.GetAll(Arg.Any<CancellationToken>())
            .Returns([remoteTransaction]);

        var processor = CreateOutboxProcessor();
        var initializer = CreateInitializer(processor);

        // Act
        await initializer.StartAsync(CancellationToken.None);

        // Assert - Outbox message was processed and sent to remote
        await transactionRepository.Received(1).AddRange(
            Arg.Is<IReadOnlyCollection<Transaction>>(txs => txs.Any(t => t.Id == pendingOutboxTxId)),
            Arg.Any<CancellationToken>());

        // Assert - Database state:
        await using var assertContext = new SqliteDbContext(options);

        // Outbox is now empty
        var remainingOutbox = await assertContext.OutboxMessages.ToListAsync();
        remainingOutbox.Should().BeEmpty();

        // Local transactions table contains ONLY remote transactions
        var currentTransactions = await assertContext.Transactions.ToListAsync();
        currentTransactions.Should().HaveCount(1);
        currentTransactions[0].Id.Should().Be(remoteTxId);
        currentTransactions[0].Amount.Should().Be(5000m);
        currentTransactions[0].CategoryId.Should().Be("salary");
        currentTransactions[0].Type.Should().Be(TransactionType.Income);
        currentTransactions[0].Note.Should().Be("Monthly salary");
    }

    [Fact]
    public async Task StopAsync_ShouldFlushOutbox()
    {
        // Arrange
        var txDto = new TransactionOutboxDto(
            Guid.NewGuid(),
            TransactionType.Expense,
            "food",
            20m,
            new DateTime(2026, 11, 3, 10, 0, 0),
            "Coffee");

        await using (var seedContext = new SqliteDbContext(options))
        {
            seedContext.OutboxMessages.Add(new OutboxMessageEntity
            {
                OperationType = OutboxOperationType.Add,
                PayloadJson = JsonSerializer.Serialize(new AddTransactionsOutboxPayload([txDto])),
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
            await seedContext.SaveChangesAsync();
        }

        var processor = CreateOutboxProcessor();
        var initializer = CreateInitializer(processor);

        // Act
        await initializer.StopAsync(CancellationToken.None);

        // Assert
        await transactionRepository.Received(1).AddRange(
            Arg.Is<IReadOnlyCollection<Transaction>>(txs => txs.Any(t => t.Id == txDto.Id)),
            Arg.Any<CancellationToken>());

        await using var assertContext = new SqliteDbContext(options);
        var remainingOutbox = await assertContext.OutboxMessages.ToListAsync();
        remainingOutbox.Should().BeEmpty();
    }

    [Fact]
    public async Task StopAsync_ShouldNotThrow_WhenExceptionOccurs()
    {
        // Arrange
        var txDto = new TransactionOutboxDto(
            Guid.NewGuid(),
            TransactionType.Expense,
            "food",
            20m,
            new DateTime(2026, 11, 3, 10, 0, 0),
            "Coffee");

        await using (var seedContext = new SqliteDbContext(options))
        {
            seedContext.OutboxMessages.Add(new OutboxMessageEntity
            {
                OperationType = OutboxOperationType.Add,
                PayloadJson = JsonSerializer.Serialize(new AddTransactionsOutboxPayload([txDto])),
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
            await seedContext.SaveChangesAsync();
        }

        transactionRepository.AddRange(Arg.Any<IReadOnlyCollection<Transaction>>(), Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("Network failure during shutdown"));

        var processor = CreateOutboxProcessor();
        var initializer = CreateInitializer(processor);

        // Act
        var act = () => initializer.StopAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    private sealed class TestDbContextFactory(DbContextOptions<SqliteDbContext> options) : IDbContextFactory<SqliteDbContext>
    {
        public SqliteDbContext CreateDbContext() => new(options);

        public Task<SqliteDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new SqliteDbContext(options));
    }
}
