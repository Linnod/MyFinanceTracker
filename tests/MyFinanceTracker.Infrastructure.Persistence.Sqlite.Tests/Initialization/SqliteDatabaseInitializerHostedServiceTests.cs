using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Data;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Entities;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Initialization;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Models;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Options;
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
    private readonly ICategoryRepository remoteCategoryRepository;
    private readonly ICategoryRepository localCategoryRepository;
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

        using var context = new SqliteDbContext(options);
        context.Database.EnsureCreated();

        contextFactory = new TestDbContextFactory(options);
        transactionRepository = Substitute.For<ITransactionRepository>();
        remoteCategoryRepository = Substitute.For<ICategoryRepository>();
        localCategoryRepository = Substitute.For<ICategoryRepository>();
        outboxLogger = Substitute.For<ILogger<OutboxProcessor>>();
        initializerLogger = Substitute.For<ILogger<SqliteDatabaseInitializerHostedService>>();

        localCategoryRepository.GetById(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var query = callInfo.Arg<string>();
                return new[] { foodCategory, salaryCategory }
                    .FirstOrDefault(c => c.Id.Equals(query, StringComparison.OrdinalIgnoreCase));
            });

        localCategoryRepository.GetByIds(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var query = callInfo.Arg<IReadOnlyCollection<string>>();
                var lookup = new HashSet<string>(query, StringComparer.OrdinalIgnoreCase);
                return new[] { foodCategory, salaryCategory }
                    .Where(c => lookup.Contains(c.Id))
                    .ToList();
            });

        remoteCategoryRepository.GetAll(Arg.Any<CancellationToken>())
            .Returns([foodCategory, salaryCategory]);

        transactionRepository.GetAll(Arg.Any<CancellationToken>())
            .Returns([]);
    }

    public void Dispose()
    {
        connection.Dispose();
    }

    [Fact]
    public async Task StartAsync_EmptyDatabase_SyncsCategoriesAndTransactionsFromRemote()
    {
        // arrange
        var remoteTxId = Guid.NewGuid();
        var remoteTransaction = new Transaction(
            remoteTxId,
            TransactionType.Income,
            salaryCategory,
            5000m,
            new DateTime(2026, 11, 5, 10, 0, 0),
            "Monthly salary");

        transactionRepository.GetAll(Arg.Any<CancellationToken>())
            .Returns([remoteTransaction]);

        var initializer = CreateInitializer();

        // act
        await initializer.StartAsync(CancellationToken.None);

        // assert
        await using var assertContext = new SqliteDbContext(options);

        var categories = await assertContext.Categories.OrderBy(c => c.Id).ToListAsync();
        categories.Should().HaveCount(2);
        categories.Select(c => c.Id).Should().BeEquivalentTo(["food", "salary"]);

        var transactions = await assertContext.Transactions.ToListAsync();
        transactions.Should().HaveCount(1);
        transactions[0].Id.Should().Be(remoteTxId);
        transactions[0].Amount.Should().Be(5000m);
        transactions[0].CategoryId.Should().Be("salary");
        transactions[0].Type.Should().Be(TransactionType.Income);
        transactions[0].Note.Should().Be("Monthly salary");
    }

    [Fact]
    public async Task StartAsync_EmptyDatabaseWithPendingOutbox_FlushesOutboxFirstThenSyncsFromRemote()
    {
        // arrange
        var pendingOutboxTxId = Guid.NewGuid();
        var remoteTxId = Guid.NewGuid();

        await SeedOutboxAddTransaction(pendingOutboxTxId, "food", 45m, "Pending outbox item");

        var remoteTransaction = new Transaction(
            remoteTxId,
            TransactionType.Income,
            salaryCategory,
            5000m,
            new DateTime(2026, 11, 5, 10, 0, 0),
            "Monthly salary");

        transactionRepository.GetAll(Arg.Any<CancellationToken>())
            .Returns([remoteTransaction]);

        var initializer = CreateInitializer();

        // act
        await initializer.StartAsync(CancellationToken.None);

        // assert
        await transactionRepository.Received(1).AddRange(
            Arg.Is<IReadOnlyCollection<Transaction>>(txs => txs.Any(t => t.Id == pendingOutboxTxId)),
            Arg.Any<CancellationToken>());

        await using var assertContext = new SqliteDbContext(options);
        (await assertContext.OutboxMessages.ToListAsync()).Should().BeEmpty();

        var currentTransactions = await assertContext.Transactions.ToListAsync();
        currentTransactions.Should().HaveCount(1);
        currentTransactions[0].Id.Should().Be(remoteTxId);
    }

    [Fact]
    public async Task StartAsync_LocalDatabaseHasCategories_SkipsRemoteSync()
    {
        // arrange
        await SeedLocalCategory(foodCategory);
        var initializer = CreateInitializer();

        // act
        await initializer.StartAsync(CancellationToken.None);

        // assert
        await remoteCategoryRepository.DidNotReceive().GetAll(Arg.Any<CancellationToken>());
        await transactionRepository.DidNotReceive().GetAll(Arg.Any<CancellationToken>());

        await using var assertContext = new SqliteDbContext(options);
        var categories = await assertContext.Categories.ToListAsync();
        categories.Should().HaveCount(1);
        categories[0].Id.Should().Be(foodCategory.Id);
    }

    [Fact]
    public async Task StartAsync_LocalDatabaseHasTransactions_FlushesOutboxAndSkipsRemoteSync()
    {
        // arrange
        var existingLocalTxId = Guid.NewGuid();
        var pendingOutboxTxId = Guid.NewGuid();

        await SeedLocalCategory(foodCategory);
        await using (var seedContext = new SqliteDbContext(options))
        {
            seedContext.Transactions.Add(new TransactionEntity
            {
                Id = existingLocalTxId,
                Type = TransactionType.Expense,
                CategoryId = foodCategory.Id,
                Amount = 99m,
                Date = new DateTime(2026, 10, 1, 10, 0, 0),
                Note = "Existing local transaction"
            });
            await seedContext.SaveChangesAsync();
        }

        await SeedOutboxAddTransaction(pendingOutboxTxId, "food", 45m, "Pending outbox item");

        var initializer = CreateInitializer();

        // act
        await initializer.StartAsync(CancellationToken.None);

        // assert
        await transactionRepository.Received(1).AddRange(
            Arg.Is<IReadOnlyCollection<Transaction>>(txs => txs.Any(t => t.Id == pendingOutboxTxId)),
            Arg.Any<CancellationToken>());
        await remoteCategoryRepository.DidNotReceive().GetAll(Arg.Any<CancellationToken>());
        await transactionRepository.DidNotReceive().GetAll(Arg.Any<CancellationToken>());

        await using var assertContext = new SqliteDbContext(options);
        (await assertContext.OutboxMessages.ToListAsync()).Should().BeEmpty();

        var transactions = await assertContext.Transactions.ToListAsync();
        transactions.Should().HaveCount(1);
        transactions[0].Id.Should().Be(existingLocalTxId);
    }

    [Fact]
    public async Task StartAsync_PendingOutboxRemainsAfterFlushFailure_SkipsRemoteSync()
    {
        // arrange
        var pendingOutboxTxId = Guid.NewGuid();
        await SeedOutboxAddTransaction(pendingOutboxTxId);

        transactionRepository.AddRange(Arg.Any<IReadOnlyCollection<Transaction>>(), Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("Temporary Google Sheets outage"));

        var initializer = CreateInitializer();

        // act
        await initializer.StartAsync(CancellationToken.None);

        // assert
        await remoteCategoryRepository.DidNotReceive().GetAll(Arg.Any<CancellationToken>());
        await transactionRepository.DidNotReceive().GetAll(Arg.Any<CancellationToken>());

        await using var assertContext = new SqliteDbContext(options);
        (await assertContext.OutboxMessages.CountAsync()).Should().Be(1);
        (await assertContext.Categories.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task StartAsync_DeadLetterMessagesExist_SkipsRemoteSync()
    {
        // arrange
        await using (var seedContext = new SqliteDbContext(options))
        {
            seedContext.DeadLetterMessages.Add(new DeadLetterMessageEntity
            {
                OriginalOutboxMessageId = 1,
                OperationType = OutboxOperationType.AddCategory,
                PayloadJson = "{}",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                FailedAtUtc = DateTimeOffset.UtcNow,
                RetryCount = 5,
                ErrorMessage = "Permanent error"
            });
            await seedContext.SaveChangesAsync();
        }

        var initializer = CreateInitializer();

        // act
        await initializer.StartAsync(CancellationToken.None);

        // assert
        await remoteCategoryRepository.DidNotReceive().GetAll(Arg.Any<CancellationToken>());
        await transactionRepository.DidNotReceive().GetAll(Arg.Any<CancellationToken>());

        await using var assertContext = new SqliteDbContext(options);
        (await assertContext.Categories.AnyAsync()).Should().BeFalse();
        (await assertContext.Transactions.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task StartAsync_RemoteTransactionsFetchFails_DoesNotPersistCategories()
    {
        // arrange
        transactionRepository.GetAll(Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("Failed to fetch transactions"));

        var initializer = CreateInitializer();

        // act
        var act = () => initializer.StartAsync(CancellationToken.None);

        // assert
        await act.Should().ThrowAsync<HttpRequestException>();

        await using var assertContext = new SqliteDbContext(options);
        (await assertContext.Categories.AnyAsync()).Should().BeFalse();
        (await assertContext.Transactions.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task StartAsync_RemoteCategoryRepositoryReturnsCaseInsensitiveDuplicates_ThrowsDbUpdateExceptionAndRollsBack()
    {
        // arrange
        var duplicateFoodCategory = new Category("FOOD", "Food Duplicate");
        remoteCategoryRepository.GetAll(Arg.Any<CancellationToken>())
            .Returns([foodCategory, duplicateFoodCategory, salaryCategory]);

        var initializer = CreateInitializer();

        // act
        var act = () => initializer.StartAsync(CancellationToken.None);

        // assert
        await act.Should().ThrowAsync<DbUpdateException>();

        await using var assertContext = new SqliteDbContext(options);
        (await assertContext.Categories.AnyAsync()).Should().BeFalse();
        (await assertContext.Transactions.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task StartAsync_RemoteTransactionRepositoryReturnsDuplicates_ThrowsInvalidOperationExceptionAndRollsBack()
    {
        // arrange
        var txId = Guid.NewGuid();
        var remoteTx1 = new Transaction(txId, TransactionType.Expense, foodCategory, 10m, DateTime.UtcNow, "note");
        var remoteTx2 = new Transaction(txId, TransactionType.Expense, foodCategory, 20m, DateTime.UtcNow, "duplicate");

        transactionRepository.GetAll(Arg.Any<CancellationToken>())
            .Returns([remoteTx1, remoteTx2]);

        var initializer = CreateInitializer();

        // act
        var act = () => initializer.StartAsync(CancellationToken.None);

        // assert
        await act.Should().ThrowAsync<InvalidOperationException>();

        await using var assertContext = new SqliteDbContext(options);
        (await assertContext.Categories.AnyAsync()).Should().BeFalse();
        (await assertContext.Transactions.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task StopAsync_ShouldFlushOutbox()
    {
        // arrange
        var txId = Guid.NewGuid();
        await SeedOutboxAddTransaction(txId);

        var initializer = CreateInitializer();

        // act
        await initializer.StopAsync(CancellationToken.None);

        // assert
        await transactionRepository.Received(1).AddRange(
            Arg.Is<IReadOnlyCollection<Transaction>>(txs => txs.Any(t => t.Id == txId)),
            Arg.Any<CancellationToken>());

        await using var assertContext = new SqliteDbContext(options);
        var remainingOutbox = await assertContext.OutboxMessages.ToListAsync();
        remainingOutbox.Should().BeEmpty();
    }

    [Fact]
    public async Task StopAsync_ShouldNotThrow_WhenExceptionOccurs()
    {
        // arrange
        await SeedOutboxAddTransaction(Guid.NewGuid());

        transactionRepository.AddRange(Arg.Any<IReadOnlyCollection<Transaction>>(), Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("Network failure during shutdown"));

        var initializer = CreateInitializer();

        // act
        var act = () => initializer.StopAsync(CancellationToken.None);

        // assert
        await act.Should().NotThrowAsync();
    }

    private sealed class TestDbContextFactory(DbContextOptions<SqliteDbContext> options) : IDbContextFactory<SqliteDbContext>
    {
        public SqliteDbContext CreateDbContext() => new(options);

        public Task<SqliteDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new SqliteDbContext(options));
    }

    private OutboxProcessor CreateOutboxProcessor() =>
        new(contextFactory,
            transactionRepository,
            remoteCategoryRepository,
            localCategoryRepository,
            TimeProvider.System,
            Microsoft.Extensions.Options.Options.Create(new SqlitePersistenceOptions()),
            Substitute.For<IOutboxNotificationPublisher>(),
            outboxLogger);

    private SqliteDatabaseInitializerHostedService CreateInitializer(OutboxProcessor? processor = null) =>
        new(contextFactory, transactionRepository, remoteCategoryRepository, processor ?? CreateOutboxProcessor(), initializerLogger);

    private async Task SeedOutboxAddTransaction(Guid txId, string categoryId = "food", decimal amount = 20m, string? note = "Coffee")
    {
        var txDto = new TransactionOutboxDto(
            txId,
            TransactionType.Expense,
            categoryId,
            amount,
            new DateTime(2026, 11, 3, 10, 0, 0),
            note);

        await using var context = new SqliteDbContext(options);
        context.OutboxMessages.Add(new OutboxMessageEntity
        {
            OperationType = OutboxOperationType.AddTransaction,
            PayloadJson = JsonSerializer.Serialize(new AddTransactionsOutboxPayload([txDto])),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();
    }

    private async Task SeedLocalCategory(Category category)
    {
        await using var context = new SqliteDbContext(options);
        context.Categories.Add(new CategoryEntity
        {
            Id = category.Id,
            Name = category.Name,
            IsArchived = category.IsArchived
        });
        await context.SaveChangesAsync();
    }
}
