using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Common.Notifications;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Options;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Data;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Entities;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Models;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Sync;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Tests.Sync;

public sealed class OutboxProcessorTests : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly DbContextOptions<SqliteDbContext> options;
    private readonly IDbContextFactory<SqliteDbContext> contextFactory;
    private readonly ITransactionRepository remoteRepository;
    private readonly ICategoryRepository remoteCategoryRepository;
    private readonly ICategoryRepository localCategoryRepository;
    private readonly TimeProvider timeProvider;
    private readonly IOutboxNotificationPublisher notificationPublisher;
    private readonly ILogger<OutboxProcessor> logger;

    private readonly Category foodCategory = new("food", "Food");
    private readonly Category transportCategory = new("transport", "Transport");

    public OutboxProcessorTests()
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
        remoteCategoryRepository = Substitute.For<ICategoryRepository>();
        localCategoryRepository = Substitute.For<ICategoryRepository>();
        timeProvider = TimeProvider.System;
        notificationPublisher = Substitute.For<IOutboxNotificationPublisher>();
        logger = Substitute.For<ILogger<OutboxProcessor>>();

        localCategoryRepository.GetById(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var query = callInfo.Arg<string>();
                return new[] { foodCategory, transportCategory }
                    .FirstOrDefault(c => c.Id.Equals(query, StringComparison.OrdinalIgnoreCase));
            });

        localCategoryRepository.GetByIds(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var query = callInfo.Arg<IReadOnlyCollection<string>>();
                var lookup = new HashSet<string>(query, StringComparer.OrdinalIgnoreCase);
                return new[] { foodCategory, transportCategory }
                    .Where(c => lookup.Contains(c.Id))
                    .ToList();
            });
    }

    public void Dispose()
    {
        connection.Dispose();
    }

    private OutboxProcessor CreateProcessor(int maxRetries = 3) =>
        new(contextFactory,
            remoteRepository,
            remoteCategoryRepository,
            localCategoryRepository,
            timeProvider,
            Microsoft.Extensions.Options.Options.Create(new SqlitePersistenceOptions { MaxRetryAttempts = maxRetries }),
            notificationPublisher,
            logger);

    [Fact]
    public async Task ProcessPendingMessages_ShouldDoNothing_WhenQueueIsEmpty()
    {
        // Arrange
        using var processor = CreateProcessor();

        // Act
        await processor.ProcessPendingMessages(CancellationToken.None);

        // Assert
        await remoteRepository.DidNotReceiveWithAnyArgs().AddRange(default!, default);
        await remoteRepository.DidNotReceiveWithAnyArgs().DeleteRange(default!, default);
    }

    [Fact]
    public async Task ProcessPendingMessages_ShouldSendAddOperations_AndRemoveMessageFromQueue()
    {
        // Arrange
        var txDto1 = new TransactionOutboxDto(
            Guid.NewGuid(),
            TransactionType.Expense,
            "food",
            150m,
            new DateTime(2026, 11, 1),
            "Groceries");

        var txDto2 = new TransactionOutboxDto(
            Guid.NewGuid(),
            TransactionType.Income,
            "transport",
            50m,
            new DateTime(2026, 11, 2),
            null);

        var payload = new AddTransactionsOutboxPayload([txDto1, txDto2]);

        await using (var seedContext = new SqliteDbContext(options))
        {
            seedContext.OutboxMessages.Add(new OutboxMessageEntity
            {
                OperationType = OutboxOperationType.AddTransaction,
                PayloadJson = JsonSerializer.Serialize(payload),
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
            await seedContext.SaveChangesAsync();
        }

        using var processor = CreateProcessor();

        // Act
        await processor.ProcessPendingMessages(CancellationToken.None);

        // Assert
        await remoteRepository.Received(1).AddRange(
            Arg.Is<IReadOnlyCollection<Transaction>>(list =>
                list.Count == 2 &&
                list.Any(t => t.Id == txDto1.Id && t.Category.Id == "food" && t.Amount == 150m) &&
                list.Any(t => t.Id == txDto2.Id && t.Category.Id == "transport" && t.Amount == 50m)),
            Arg.Any<CancellationToken>());

        await using var assertContext = new SqliteDbContext(options);
        var remainingMessages = await assertContext.OutboxMessages.ToListAsync();
        remainingMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessPendingMessages_ShouldDeleteRangeSuccessfully_WhenAllIdsFound()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var payload = new DeleteTransactionsOutboxPayload([id1, id2]);

        await using (var seedContext = new SqliteDbContext(options))
        {
            seedContext.OutboxMessages.Add(new OutboxMessageEntity
            {
                OperationType = OutboxOperationType.DeleteTransaction,
                PayloadJson = JsonSerializer.Serialize(payload),
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
            await seedContext.SaveChangesAsync();
        }

        remoteRepository.DeleteRange(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(2);

        using var processor = CreateProcessor();

        // Act
        await processor.ProcessPendingMessages(CancellationToken.None);

        // Assert
        await remoteRepository.Received(1).DeleteRange(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 2 && ids.Contains(id1) && ids.Contains(id2)),
            Arg.Any<CancellationToken>());

        await using var assertContext = new SqliteDbContext(options);
        var remainingMessages = await assertContext.OutboxMessages.ToListAsync();
        remainingMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessPendingMessages_ShouldIncrementRetryCountAndStopBatch_WhenRemoteThrows_AndAttemptsBelowMax()
    {
        // Arrange - Seed 2 messages. First fails, second shouldn't be processed this tick.
        var txDto1 = new TransactionOutboxDto(Guid.NewGuid(), TransactionType.Expense, "food", 100m, new DateTime(2026, 11, 1), "Test1");
        var txDto2 = new TransactionOutboxDto(Guid.NewGuid(), TransactionType.Expense, "food", 200m, new DateTime(2026, 11, 2), "Test2");

        await using (var seedContext = new SqliteDbContext(options))
        {
            seedContext.OutboxMessages.AddRange(
                new OutboxMessageEntity { Id = 1, OperationType = OutboxOperationType.AddTransaction, PayloadJson = JsonSerializer.Serialize(new AddTransactionsOutboxPayload([txDto1])), CreatedAtUtc = DateTimeOffset.UtcNow, RetryCount = 0 },
                new OutboxMessageEntity { Id = 2, OperationType = OutboxOperationType.AddTransaction, PayloadJson = JsonSerializer.Serialize(new AddTransactionsOutboxPayload([txDto2])), CreatedAtUtc = DateTimeOffset.UtcNow, RetryCount = 0 }
            );
            await seedContext.SaveChangesAsync();
        }

        remoteRepository.AddRange(Arg.Any<IReadOnlyCollection<Transaction>>(), Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("Remote network error"));

        using var processor = CreateProcessor(maxRetries: 3);

        // Act - should not throw, but handle gracefully
        await processor.ProcessPendingMessages(CancellationToken.None);

        // Assert - Message 1 remains in OutboxMessages with RetryCount = 1
        await using var assertContext = new SqliteDbContext(options);
        var remainingMessages = await assertContext.OutboxMessages.OrderBy(m => m.Id).ToListAsync();
        remainingMessages.Should().HaveCount(2);

        remainingMessages[0].Id.Should().Be(1);
        remainingMessages[0].RetryCount.Should().Be(1);

        // Message 2 was NOT touched during this tick
        remainingMessages[1].Id.Should().Be(2);
        remainingMessages[1].RetryCount.Should().Be(0);

        // No dead letter messages
        var deadLetter = await assertContext.DeadLetterMessages.ToListAsync();
        deadLetter.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessPendingMessages_ShouldMoveToDeadLetterAndContinue_WhenAttemptsExceedMax()
    {
        // Arrange - Message 1 has reached RetryCount = 2 (max = 3). Next failure sends it to DLQ.
        // Message 2 is valid and should be processed successfully in the same run!
        var txDto1 = new TransactionOutboxDto(Guid.NewGuid(), TransactionType.Expense, "food", 100m, new DateTime(2026, 11, 1), "FailedItem");
        var txDto2 = new TransactionOutboxDto(Guid.NewGuid(), TransactionType.Expense, "food", 200m, new DateTime(2026, 11, 2), "ValidItem");

        await using (var seedContext = new SqliteDbContext(options))
        {
            seedContext.OutboxMessages.AddRange(
                new OutboxMessageEntity { Id = 1, OperationType = OutboxOperationType.AddTransaction, PayloadJson = JsonSerializer.Serialize(new AddTransactionsOutboxPayload([txDto1])), CreatedAtUtc = DateTimeOffset.UtcNow, RetryCount = 2 },
                new OutboxMessageEntity { Id = 2, OperationType = OutboxOperationType.AddTransaction, PayloadJson = JsonSerializer.Serialize(new AddTransactionsOutboxPayload([txDto2])), CreatedAtUtc = DateTimeOffset.UtcNow, RetryCount = 0 }
            );
            await seedContext.SaveChangesAsync();
        }

        remoteRepository.AddRange(
            Arg.Is<IReadOnlyCollection<Transaction>>(txs => txs.Any(t => t.Id == txDto1.Id)),
            Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Fatal API format error"));

        remoteRepository.AddRange(
            Arg.Is<IReadOnlyCollection<Transaction>>(txs => txs.Any(t => t.Id == txDto2.Id)),
            Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        using var processor = CreateProcessor(maxRetries: 3);

        // Act
        await processor.ProcessPendingMessages(CancellationToken.None);

        // Assert
        await using var assertContext = new SqliteDbContext(options);

        // Outbox is empty because Message 1 went to Dead Letter and Message 2 succeeded!
        var remainingOutbox = await assertContext.OutboxMessages.ToListAsync();
        remainingOutbox.Should().BeEmpty();

        // Message 1 is in DeadLetterMessages
        var deadLetters = await assertContext.DeadLetterMessages.ToListAsync();
        deadLetters.Should().HaveCount(1);
        deadLetters[0].OriginalOutboxMessageId.Should().Be(1);
        deadLetters[0].RetryCount.Should().Be(3);
        deadLetters[0].ErrorMessage.Should().Contain("Fatal API format error");

        // Message 2 was sent to remote
        await remoteRepository.Received(1).AddRange(
            Arg.Is<IReadOnlyCollection<Transaction>>(txs => txs.Any(t => t.Id == txDto2.Id)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessPendingMessages_ShouldMoveToDeadLetter_WhenCategoryNotFound()
    {
        // Arrange - Seed message with non-existent category
        var txDto = new TransactionOutboxDto(Guid.NewGuid(), TransactionType.Expense, "non_existent_category", 50m, new DateTime(2026, 11, 1), null);

        await using (var seedContext = new SqliteDbContext(options))
        {
            seedContext.OutboxMessages.Add(
                new OutboxMessageEntity { Id = 10, OperationType = OutboxOperationType.AddTransaction, PayloadJson = JsonSerializer.Serialize(new AddTransactionsOutboxPayload([txDto])), CreatedAtUtc = DateTimeOffset.UtcNow, RetryCount = 0 }
            );
            await seedContext.SaveChangesAsync();
        }

        // Configure max retries to 1 so it immediately goes to Dead Letter
        using var processor = CreateProcessor(maxRetries: 1);

        // Act
        await processor.ProcessPendingMessages(CancellationToken.None);

        // Assert
        await using var assertContext = new SqliteDbContext(options);
        var outbox = await assertContext.OutboxMessages.ToListAsync();
        outbox.Should().BeEmpty();

        var deadLetters = await assertContext.DeadLetterMessages.ToListAsync();
        deadLetters.Should().HaveCount(1);
        deadLetters[0].OriginalOutboxMessageId.Should().Be(10);
        deadLetters[0].ErrorMessage.Should().Contain("Category 'non_existent_category'");
    }

    [Fact]
    public async Task ProcessPendingMessages_ShouldRethrowOperationCanceled_AndNotIncrementRetryCount_WhenCancelled()
    {
        // Arrange
        var txDto = new TransactionOutboxDto(Guid.NewGuid(), TransactionType.Expense, "food", 50m, new DateTime(2026, 11, 1), null);

        await using (var seedContext = new SqliteDbContext(options))
        {
            seedContext.OutboxMessages.Add(
                new OutboxMessageEntity { Id = 20, OperationType = OutboxOperationType.AddTransaction, PayloadJson = JsonSerializer.Serialize(new AddTransactionsOutboxPayload([txDto])), CreatedAtUtc = DateTimeOffset.UtcNow, RetryCount = 0 }
            );
            await seedContext.SaveChangesAsync();
        }

        using var cts = new CancellationTokenSource();

        remoteRepository.AddRange(Arg.Any<IReadOnlyCollection<Transaction>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cts.Cancel();
                cts.Token.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            });

        using var processor = CreateProcessor();

        // Act
        var act = () => processor.ProcessPendingMessages(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();

        await using var assertContext = new SqliteDbContext(options);
        var message = await assertContext.OutboxMessages.FindAsync(20L);
        message.Should().NotBeNull();
        message!.RetryCount.Should().Be(0); // Retry count was NOT incremented!
    }

    [Fact]
    public async Task ProcessPendingMessages_ShouldMoveToDeadLetter_WhenJsonIsMalformed()
    {
        // Arrange
        await using (var seedContext = new SqliteDbContext(options))
        {
            seedContext.OutboxMessages.Add(
                new OutboxMessageEntity { Id = 30, OperationType = OutboxOperationType.AddTransaction, PayloadJson = "{ malformed json", CreatedAtUtc = DateTimeOffset.UtcNow, RetryCount = 0 }
            );
            await seedContext.SaveChangesAsync();
        }

        using var processor = CreateProcessor(maxRetries: 1);

        // Act
        await processor.ProcessPendingMessages(CancellationToken.None);

        // Assert
        await using var assertContext = new SqliteDbContext(options);
        var outbox = await assertContext.OutboxMessages.ToListAsync();
        outbox.Should().BeEmpty();

        var deadLetters = await assertContext.DeadLetterMessages.ToListAsync();
        deadLetters.Should().HaveCount(1);
        deadLetters[0].OriginalOutboxMessageId.Should().Be(30);
        deadLetters[0].ErrorMessage.Should().Contain("Failed to deserialize message payload");
        deadLetters[0].ErrorMessage.Should().Contain("JsonException");
    }

    [Fact]
    public async Task ProcessPendingMessages_MessageMovedToDeadLetter_PublishesMessageMovedToDeadLetterNotification()
    {
        // arrange
        var txDto = new TransactionOutboxDto(Guid.NewGuid(), TransactionType.Expense, "food", 100m, new DateTime(2026, 11, 1), "Fail");

        await using (var seedContext = new SqliteDbContext(options))
        {
            seedContext.OutboxMessages.Add(new OutboxMessageEntity
            {
                Id = 42,
                OperationType = OutboxOperationType.AddTransaction,
                PayloadJson = JsonSerializer.Serialize(new AddTransactionsOutboxPayload([txDto])),
                CreatedAtUtc = DateTimeOffset.UtcNow,
                RetryCount = 2
            });
            await seedContext.SaveChangesAsync();
        }

        remoteRepository.AddRange(Arg.Any<IReadOnlyCollection<Transaction>>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Permanent remote error"));

        using var processor = CreateProcessor(maxRetries: 3);

        // act
        await processor.ProcessPendingMessages(CancellationToken.None);

        // assert
        await notificationPublisher.Received(1).Publish(
            Arg.Is<OutboxSyncNotification.MessageMovedToDeadLetter>(n =>
                n.OriginalOutboxMessageId == 42 &&
                n.OperationType == "AddTransaction" &&
                n.ErrorMessage == "Permanent remote error"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessPendingMessages_RetryAttemptBelowMax_DoesNotPublishNotification()
    {
        // arrange
        var txDto = new TransactionOutboxDto(Guid.NewGuid(), TransactionType.Expense, "food", 100m, new DateTime(2026, 11, 1), "Test");

        await using (var seedContext = new SqliteDbContext(options))
        {
            seedContext.OutboxMessages.Add(new OutboxMessageEntity
            {
                Id = 1,
                OperationType = OutboxOperationType.AddTransaction,
                PayloadJson = JsonSerializer.Serialize(new AddTransactionsOutboxPayload([txDto])),
                CreatedAtUtc = DateTimeOffset.UtcNow,
                RetryCount = 0
            });
            await seedContext.SaveChangesAsync();
        }

        remoteRepository.AddRange(Arg.Any<IReadOnlyCollection<Transaction>>(), Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("Temporary network error"));

        using var processor = CreateProcessor(maxRetries: 3);

        // act
        await processor.ProcessPendingMessages(CancellationToken.None);

        // assert
        await notificationPublisher.DidNotReceiveWithAnyArgs().Publish(default!, default);
    }

    [Fact]
    public async Task ProcessPendingMessages_ShouldSendUpdateOperation_AndRemoveMessageFromQueue()
    {
        // arrange
        var txDto = new TransactionOutboxDto(
            Guid.NewGuid(),
            TransactionType.Income,
            "food",
            200m,
            new DateTime(2026, 11, 4),
            "Updated note");

        var payload = new UpdateTransactionOutboxPayload(txDto);

        await using (var seedContext = new SqliteDbContext(options))
        {
            seedContext.OutboxMessages.Add(new OutboxMessageEntity
            {
                OperationType = OutboxOperationType.UpdateTransaction,
                PayloadJson = JsonSerializer.Serialize(payload),
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
            await seedContext.SaveChangesAsync();
        }

        using var processor = CreateProcessor();

        // act
        await processor.ProcessPendingMessages(CancellationToken.None);

        // assert
        await remoteRepository.Received(1).Update(
            Arg.Is<Transaction>(t =>
                t.Id == txDto.Id &&
                t.Type == TransactionType.Income &&
                t.Category.Id == "food" &&
                t.Amount == 200m &&
                t.Date == new DateTime(2026, 11, 4) &&
                t.Note == "Updated note"),
            Arg.Any<CancellationToken>());

        await using var assertContext = new SqliteDbContext(options);
        var remainingMessages = await assertContext.OutboxMessages.ToListAsync();
        remainingMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessPendingMessages_UpdateWithUnknownCategory_ShouldMoveToDeadLetterAfterMaxRetries()
    {
        // arrange
        var txDto = new TransactionOutboxDto(
            Guid.NewGuid(),
            TransactionType.Expense,
            "unknown_category",
            75m,
            new DateTime(2026, 11, 4),
            null);

        var payload = new UpdateTransactionOutboxPayload(txDto);

        await using (var seedContext = new SqliteDbContext(options))
        {
            seedContext.OutboxMessages.Add(new OutboxMessageEntity
            {
                Id = 50,
                OperationType = OutboxOperationType.UpdateTransaction,
                PayloadJson = JsonSerializer.Serialize(payload),
                CreatedAtUtc = DateTimeOffset.UtcNow,
                RetryCount = 2
            });
            await seedContext.SaveChangesAsync();
        }

        using var processor = CreateProcessor(maxRetries: 3);

        // act
        await processor.ProcessPendingMessages(CancellationToken.None);

        // assert
        await using var assertContext = new SqliteDbContext(options);
        var outbox = await assertContext.OutboxMessages.ToListAsync();
        outbox.Should().BeEmpty();

        var deadLetters = await assertContext.DeadLetterMessages.ToListAsync();
        deadLetters.Should().HaveCount(1);
        deadLetters[0].OriginalOutboxMessageId.Should().Be(50);
        deadLetters[0].RetryCount.Should().Be(3);
        deadLetters[0].ErrorMessage.Should().Contain("Category 'unknown_category'");

        await remoteRepository.DidNotReceiveWithAnyArgs().Update(default!, default);
    }

    [Fact]
    public async Task ProcessPendingMessages_AddCategoryOperation_CallsRemoteCategoryRepositoryAndRemovesMessage()
    {
        // arrange
        using var processor = CreateProcessor();
        var categoryDto = new CategoryOutboxDto("shopping", "Shopping", false);
        var payload = new AddCategoryOutboxPayload(categoryDto);

        await using (var seedContext = new SqliteDbContext(options))
        {
            seedContext.OutboxMessages.Add(new OutboxMessageEntity
            {
                Id = 1,
                OperationType = OutboxOperationType.AddCategory,
                PayloadJson = JsonSerializer.Serialize(payload),
                CreatedAtUtc = DateTimeOffset.UtcNow,
                RetryCount = 0
            });
            await seedContext.SaveChangesAsync();
        }

        // act
        await processor.ProcessPendingMessages(CancellationToken.None);

        // assert
        await remoteCategoryRepository.Received(1).Add(
            Arg.Is<Category>(c => c.Id == "shopping" && c.Name == "Shopping" && !c.IsArchived),
            Arg.Any<CancellationToken>());

        await using var assertContext = new SqliteDbContext(options);
        var messages = await assertContext.OutboxMessages.ToListAsync();
        messages.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessPendingMessages_UpdateCategoryOperation_CallsRemoteCategoryRepositoryAndRemovesMessage()
    {
        // arrange
        using var processor = CreateProcessor();
        var categoryDto = new CategoryOutboxDto("shopping", "Shopping & Supermarket", true);
        var payload = new UpdateCategoryOutboxPayload(categoryDto);

        await using (var seedContext = new SqliteDbContext(options))
        {
            seedContext.OutboxMessages.Add(new OutboxMessageEntity
            {
                Id = 1,
                OperationType = OutboxOperationType.UpdateCategory,
                PayloadJson = JsonSerializer.Serialize(payload),
                CreatedAtUtc = DateTimeOffset.UtcNow,
                RetryCount = 0
            });
            await seedContext.SaveChangesAsync();
        }

        // act
        await processor.ProcessPendingMessages(CancellationToken.None);

        // assert
        await remoteCategoryRepository.Received(1).Update(
            Arg.Is<Category>(c => c.Id == "shopping" && c.Name == "Shopping & Supermarket" && c.IsArchived),
            Arg.Any<CancellationToken>());

        await using var assertContext = new SqliteDbContext(options);
        var messages = await assertContext.OutboxMessages.ToListAsync();
        messages.Should().BeEmpty();
    }

    private sealed class TestDbContextFactory(DbContextOptions<SqliteDbContext> options) : IDbContextFactory<SqliteDbContext>
    {
        public SqliteDbContext CreateDbContext() => new(options);

        public Task<SqliteDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new SqliteDbContext(options));
    }
}
