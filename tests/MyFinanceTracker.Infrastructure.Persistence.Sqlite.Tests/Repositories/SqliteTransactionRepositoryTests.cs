using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Data;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Entities;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Exceptions;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Models;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Repositories;
using NSubstitute;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Tests.Repositories;

public sealed class SqliteTransactionRepositoryTests : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly DbContextOptions<SqliteDbContext> options;
    private readonly IDbContextFactory<SqliteDbContext> contextFactory;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<SqliteTransactionRepository> logger;
    private readonly DateTimeOffset fixedTime = new(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);

    private readonly Category foodCategory = new("food", "Food");
    private readonly Category transportCategory = new("transport", "Transport");
    private readonly Category salaryCategory = new("salary", "Salary");

    public SqliteTransactionRepositoryTests()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        options = new DbContextOptionsBuilder<SqliteDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new SqliteDbContext(options);
        context.Database.EnsureCreated();

        context.Categories.AddRange(
            new CategoryEntity { Id = foodCategory.Id, Name = foodCategory.Name, IsArchived = foodCategory.IsArchived },
            new CategoryEntity { Id = transportCategory.Id, Name = transportCategory.Name, IsArchived = transportCategory.IsArchived },
            new CategoryEntity { Id = salaryCategory.Id, Name = salaryCategory.Name, IsArchived = salaryCategory.IsArchived });
        context.SaveChanges();

        contextFactory = new TestDbContextFactory(options);
        timeProvider = new FixedTimeProvider(fixedTime);
        logger = Substitute.For<ILogger<SqliteTransactionRepository>>();
    }

    public void Dispose()
    {
        connection.Dispose();
    }

    private SqliteTransactionRepository CreateRepository() =>
        new(contextFactory, timeProvider, logger);

    [Fact]
    public async Task AddRange_ShouldPersistTransactionsAndCreateOutboxMessage()
    {
        // Arrange
        var repository = CreateRepository();
        var tx1 = new Transaction(
            Guid.NewGuid(),
            TransactionType.Expense,
            foodCategory,
            120.50m,
            new DateTime(2026, 11, 1, 14, 30, 0),
            "Supermarket");

        var tx2 = new Transaction(
            Guid.NewGuid(),
            TransactionType.Income,
            salaryCategory,
            3000.00m,
            new DateTime(2026, 11, 2, 9, 15, 0),
            null);

        // Act
        await repository.AddRange([tx1, tx2], CancellationToken.None);

        // Assert - Verify Transactions table
        await using var assertContext = new SqliteDbContext(options);
        var transactions = await assertContext.Transactions.OrderBy(t => t.Date).ToListAsync();

        transactions.Should().HaveCount(2);

        transactions[0].Id.Should().Be(tx1.Id);
        transactions[0].Type.Should().Be(TransactionType.Expense);
        transactions[0].CategoryId.Should().Be("food");
        transactions[0].Amount.Should().Be(120.50m);
        transactions[0].Date.Should().Be(new DateTime(2026, 11, 1, 14, 30, 0));
        transactions[0].Note.Should().Be("Supermarket");

        transactions[1].Id.Should().Be(tx2.Id);
        transactions[1].Type.Should().Be(TransactionType.Income);
        transactions[1].CategoryId.Should().Be("salary");
        transactions[1].Amount.Should().Be(3000.00m);
        transactions[1].Date.Should().Be(new DateTime(2026, 11, 2, 9, 15, 0));
        transactions[1].Note.Should().BeNull();

        // Assert - Verify OutboxMessages table
        var outboxMessages = await assertContext.OutboxMessages.ToListAsync();
        outboxMessages.Should().HaveCount(1);

        var outboxMessage = outboxMessages[0];
        outboxMessage.OperationType.Should().Be(OutboxOperationType.AddTransaction);
        outboxMessage.CreatedAtUtc.Should().Be(fixedTime);

        var payload = JsonSerializer.Deserialize<AddTransactionsOutboxPayload>(outboxMessage.PayloadJson);
        payload.Should().NotBeNull();
        payload!.Transactions.Should().HaveCount(2);

        payload.Transactions[0].Should().BeEquivalentTo(new TransactionOutboxDto(
            tx1.Id,
            TransactionType.Expense,
            "food",
            120.50m,
            new DateTime(2026, 11, 1, 14, 30, 0),
            "Supermarket"));

        payload.Transactions[1].Should().BeEquivalentTo(new TransactionOutboxDto(
            tx2.Id,
            TransactionType.Income,
            "salary",
            3000.00m,
            new DateTime(2026, 11, 2, 9, 15, 0),
            null));
    }

    [Fact]
    public async Task AddRange_ShouldDoNothing_WhenListIsEmpty()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        await repository.AddRange([], CancellationToken.None);

        // Assert
        await using var assertContext = new SqliteDbContext(options);
        var transactions = await assertContext.Transactions.ToListAsync();
        var outboxMessages = await assertContext.OutboxMessages.ToListAsync();

        transactions.Should().BeEmpty();
        outboxMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_ShouldFilterByDateRangeCategoriesAndType()
    {
        // Arrange
        var repository = CreateRepository();

        var tx1 = new Transaction(Guid.NewGuid(), TransactionType.Expense, foodCategory, 50m, new DateTime(2026, 11, 1, 12, 0, 0), "Lunch");
        var tx2 = new Transaction(Guid.NewGuid(), TransactionType.Expense, foodCategory, 75m, new DateTime(2026, 11, 5, 18, 0, 0), "Dinner");
        var tx3 = new Transaction(Guid.NewGuid(), TransactionType.Expense, transportCategory, 30m, new DateTime(2026, 11, 5, 8, 30, 0), "Bus ticket");
        var tx4 = new Transaction(Guid.NewGuid(), TransactionType.Income, salaryCategory, 2500m, new DateTime(2026, 11, 5, 9, 0, 0), "Paycheck");
        var tx5 = new Transaction(Guid.NewGuid(), TransactionType.Expense, foodCategory, 100m, new DateTime(2026, 11, 10, 15, 0, 0), "Groceries");

        await repository.AddRange([tx1, tx2, tx3, tx4, tx5], CancellationToken.None);

        // Act & Assert 1: Filter by Date Range
        var dateFilter = new TransactionFilter(
            FromDate: new DateTime(2026, 11, 2, 0, 0, 0),
            ToDate: new DateTime(2026, 11, 6, 23, 59, 59));
        var dateResults = await repository.Get(dateFilter, CancellationToken.None);
        dateResults.Items.Select(t => t.Id).Should().BeEquivalentTo([tx2.Id, tx3.Id, tx4.Id]);

        // Act & Assert 2: Filter by Type
        var typeFilter = new TransactionFilter(Type: TransactionType.Income);
        var typeResults = await repository.Get(typeFilter, CancellationToken.None);
        typeResults.Items.Select(t => t.Id).Should().BeEquivalentTo([tx4.Id]);

        // Act & Assert 3: Filter by Categories
        var categoryFilter = new TransactionFilter(Categories: [foodCategory]);
        var categoryResults = await repository.Get(categoryFilter, CancellationToken.None);
        categoryResults.Items.Select(t => t.Id).Should().BeEquivalentTo([tx1.Id, tx2.Id, tx5.Id]);

        // Act & Assert 4: Filter combined
        var combinedFilter = new TransactionFilter(
            FromDate: new DateTime(2026, 11, 5, 0, 0, 0),
            ToDate: new DateTime(2026, 11, 5, 23, 59, 59),
            Categories: [foodCategory],
            Type: TransactionType.Expense);
        var combinedResults = await repository.Get(combinedFilter, CancellationToken.None);

        combinedResults.Items.Should().HaveCount(1);
        var singleResult = combinedResults.Items[0];
        singleResult.Id.Should().Be(tx2.Id);
        singleResult.Amount.Should().Be(75m);
        singleResult.Category.Should().Be(foodCategory);

        // Act & Assert 5: Filter by multiple categories
        var multiCategoryFilter = new TransactionFilter(Categories: [foodCategory, transportCategory]);
        var multiCategoryResults = await repository.Get(multiCategoryFilter, CancellationToken.None);
        multiCategoryResults.Items.Select(t => t.Id).Should().BeEquivalentTo([tx1.Id, tx2.Id, tx3.Id, tx5.Id]);
    }

    [Fact]
    public async Task Get_FilteredByExactTimeWindow_ReturnsOnlyTransactionsWithinHour()
    {
        // arrange
        var repository = CreateRepository();
        var baseDate = new DateTime(2026, 11, 8, 12, 0, 0);

        var tx1 = new Transaction(Guid.NewGuid(), TransactionType.Expense, foodCategory, 10m, baseDate.AddMinutes(-90), "90 mins ago");
        var tx2 = new Transaction(Guid.NewGuid(), TransactionType.Expense, foodCategory, 20m, baseDate.AddMinutes(-45), "45 mins ago");
        var tx3 = new Transaction(Guid.NewGuid(), TransactionType.Expense, foodCategory, 30m, baseDate.AddMinutes(-10), "10 mins ago");
        var tx4 = new Transaction(Guid.NewGuid(), TransactionType.Expense, foodCategory, 40m, baseDate.AddMinutes(10), "10 mins in future");

        await repository.AddRange([tx1, tx2, tx3, tx4], CancellationToken.None);

        var filter = new TransactionFilter(
            FromDate: baseDate.AddHours(-1),
            ToDate: baseDate);

        // act
        var results = await repository.Get(filter, CancellationToken.None);

        // assert
        results.Items.Select(t => t.Id).Should().BeEquivalentTo([tx2.Id, tx3.Id]);
    }

    [Fact]
    public async Task Get_WithPagination_ReturnsCorrectPageAndTotalCount()
    {
        // arrange
        var repository = CreateRepository();
        var tx1 = new Transaction(Guid.NewGuid(), TransactionType.Expense, foodCategory, 10m, new DateTime(2026, 11, 1, 10, 0, 0), "1");
        var tx2 = new Transaction(Guid.NewGuid(), TransactionType.Expense, foodCategory, 20m, new DateTime(2026, 11, 2, 10, 0, 0), "2");
        var tx3 = new Transaction(Guid.NewGuid(), TransactionType.Expense, foodCategory, 30m, new DateTime(2026, 11, 3, 10, 0, 0), "3");
        var tx4 = new Transaction(Guid.NewGuid(), TransactionType.Expense, foodCategory, 40m, new DateTime(2026, 11, 4, 10, 0, 0), "4");
        var tx5 = new Transaction(Guid.NewGuid(), TransactionType.Expense, foodCategory, 50m, new DateTime(2026, 11, 5, 10, 0, 0), "5");

        await repository.AddRange([tx1, tx2, tx3, tx4, tx5], CancellationToken.None);

        var filter = new TransactionFilter(Skip: 1, Take: 2);

        // act
        var results = await repository.Get(filter, CancellationToken.None);

        // assert: ordered by Date desc, so 5, 4, 3, 2, 1. Skip 1, Take 2 => [tx4, tx3]
        results.TotalCount.Should().Be(5);
        results.Items.Should().HaveCount(2);
        results.Items[0].Id.Should().Be(tx4.Id);
        results.Items[1].Id.Should().Be(tx3.Id);
    }

    [Fact]
    public async Task Get_WithTakeZero_ReturnsEmptyItemsAndTotalCount()
    {
        // arrange
        var repository = CreateRepository();
        var tx1 = new Transaction(Guid.NewGuid(), TransactionType.Expense, foodCategory, 10m, new DateTime(2026, 11, 1, 10, 0, 0), "1");
        var tx2 = new Transaction(Guid.NewGuid(), TransactionType.Expense, foodCategory, 20m, new DateTime(2026, 11, 2, 10, 0, 0), "2");

        await repository.AddRange([tx1, tx2], CancellationToken.None);

        var filter = new TransactionFilter(Skip: 0, Take: 0);

        // act
        var results = await repository.Get(filter, CancellationToken.None);

        // assert
        results.TotalCount.Should().Be(2);
        results.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_WithSkipBeyondCount_ReturnsEmptyItemsAndTotalCount()
    {
        // arrange
        var repository = CreateRepository();
        var tx1 = new Transaction(Guid.NewGuid(), TransactionType.Expense, foodCategory, 10m, new DateTime(2026, 11, 1, 10, 0, 0), "1");

        await repository.AddRange([tx1], CancellationToken.None);

        var filter = new TransactionFilter(Skip: 10, Take: 5);

        // act
        var results = await repository.Get(filter, CancellationToken.None);

        // assert
        results.TotalCount.Should().Be(1);
        results.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_ReturnsAllTransactionsOrderedByDateDescending()
    {
        // arrange
        var repository = CreateRepository();
        var tx1 = new Transaction(Guid.NewGuid(), TransactionType.Expense, foodCategory, 10m, new DateTime(2026, 11, 1, 10, 0, 0), "1");
        var tx2 = new Transaction(Guid.NewGuid(), TransactionType.Expense, foodCategory, 20m, new DateTime(2026, 11, 2, 10, 0, 0), "2");

        await repository.AddRange([tx1, tx2], CancellationToken.None);

        // act
        var results = await repository.GetAll(CancellationToken.None);

        // assert: ordered by Date desc
        results.Should().HaveCount(2);
        results[0].Id.Should().Be(tx2.Id);
        results[1].Id.Should().Be(tx1.Id);
    }

    [Fact]
    public async Task DeleteRange_ShouldRemoveTransactionsAndCreateOutboxMessage_WhenAllIdsExist()
    {
        // Arrange
        var repository = CreateRepository();

        var tx1 = new Transaction(Guid.NewGuid(), TransactionType.Expense, foodCategory, 50m, new DateTime(2026, 11, 1, 10, 0, 0), null);
        var tx2 = new Transaction(Guid.NewGuid(), TransactionType.Expense, foodCategory, 75m, new DateTime(2026, 11, 2, 11, 0, 0), null);
        var tx3 = new Transaction(Guid.NewGuid(), TransactionType.Expense, transportCategory, 30m, new DateTime(2026, 11, 3, 12, 0, 0), null);

        await repository.AddRange([tx1, tx2, tx3], CancellationToken.None);

        // Clear outbox messages from AddRange to focus only on DeleteRange
        await using (var clearOutboxContext = new SqliteDbContext(options))
        {
            clearOutboxContext.OutboxMessages.RemoveRange(clearOutboxContext.OutboxMessages);
            await clearOutboxContext.SaveChangesAsync();
        }

        // Act - Delete tx1 and tx2 (with duplicate ID in request to ensure Distinct handling)
        var result = await repository.DeleteRange([tx1.Id, tx2.Id, tx1.Id], CancellationToken.None);

        // Assert Result
        result.Should().Be(2);

        // Assert DB - Transactions
        await using var assertContext = new SqliteDbContext(options);
        var remainingTransactions = await assertContext.Transactions.ToListAsync();
        remainingTransactions.Should().HaveCount(1);
        remainingTransactions[0].Id.Should().Be(tx3.Id);

        // Assert DB - OutboxMessages
        var outboxMessages = await assertContext.OutboxMessages.ToListAsync();
        outboxMessages.Should().HaveCount(1);

        var outboxMessage = outboxMessages[0];
        outboxMessage.OperationType.Should().Be(OutboxOperationType.DeleteTransaction);
        outboxMessage.CreatedAtUtc.Should().Be(fixedTime);

        var payload = JsonSerializer.Deserialize<DeleteTransactionsOutboxPayload>(outboxMessage.PayloadJson);
        payload.Should().NotBeNull();
        payload!.Ids.Should().BeEquivalentTo([tx1.Id, tx2.Id]);
    }

    [Fact]
    public async Task DeleteRange_ShouldDeleteExistingEntitiesAndIgnoreMissingIds()
    {
        // Arrange
        var repository = CreateRepository();

        var tx1 = new Transaction(Guid.NewGuid(), TransactionType.Expense, foodCategory, 50m, new DateTime(2026, 11, 1, 10, 0, 0), null);
        var tx2 = new Transaction(Guid.NewGuid(), TransactionType.Expense, transportCategory, 30m, new DateTime(2026, 11, 2, 11, 0, 0), null);

        await repository.AddRange([tx1, tx2], CancellationToken.None);

        var missingId = Guid.NewGuid();

        // Clear outbox to verify delete outbox message is written only for existing entity
        await using (var clearOutboxContext = new SqliteDbContext(options))
        {
            clearOutboxContext.OutboxMessages.RemoveRange(clearOutboxContext.OutboxMessages);
            await clearOutboxContext.SaveChangesAsync();
        }

        // Act
        var result = await repository.DeleteRange([tx1.Id, missingId], CancellationToken.None);

        // Assert Result
        result.Should().Be(1);

        // Assert DB - tx1 deleted, tx2 remains
        await using var assertContext = new SqliteDbContext(options);
        var transactions = await assertContext.Transactions.ToListAsync();
        transactions.Should().HaveCount(1);
        transactions[0].Id.Should().Be(tx2.Id);

        // Assert DB - OutboxMessages created only for tx1
        var outboxMessages = await assertContext.OutboxMessages.ToListAsync();
        outboxMessages.Should().HaveCount(1);
        var payload = JsonSerializer.Deserialize<DeleteTransactionsOutboxPayload>(outboxMessages[0].PayloadJson);
        payload.Should().NotBeNull();
        payload!.Ids.Should().BeEquivalentTo([tx1.Id]);
    }

    [Fact]
    public async Task DeleteRange_ShouldDoNothing_WhenIdsIsEmpty()
    {
        // Arrange
        var repository = CreateRepository();
        var tx1 = new Transaction(Guid.NewGuid(), TransactionType.Expense, foodCategory, 50m, new DateTime(2026, 11, 1, 10, 0, 0), null);

        await repository.AddRange([tx1], CancellationToken.None);

        await using (var clearOutboxContext = new SqliteDbContext(options))
        {
            clearOutboxContext.OutboxMessages.RemoveRange(clearOutboxContext.OutboxMessages);
            await clearOutboxContext.SaveChangesAsync();
        }

        // Act
        var result = await repository.DeleteRange([], CancellationToken.None);

        // Assert Result
        result.Should().Be(0);

        // Assert DB - Transactions unchanged
        await using var assertContext = new SqliteDbContext(options);
        var transactions = await assertContext.Transactions.ToListAsync();
        transactions.Should().HaveCount(1);

        // Assert DB - No OutboxMessages created
        var outboxMessages = await assertContext.OutboxMessages.ToListAsync();
        outboxMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task GetById_ExistingTransaction_ReturnsTransaction()
    {
        // arrange
        var repository = CreateRepository();
        var tx = new Transaction(
            Guid.NewGuid(),
            TransactionType.Expense,
            foodCategory,
            45.50m,
            new DateTime(2026, 11, 3, 16, 20, 0),
            "Coffee and cake");

        await repository.AddRange([tx], CancellationToken.None);

        // act
        var result = await repository.GetById(tx.Id, CancellationToken.None);

        // assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(tx.Id);
        result.Type.Should().Be(TransactionType.Expense);
        result.Category.Should().Be(foodCategory);
        result.Amount.Should().Be(45.50m);
        result.Date.Should().Be(new DateTime(2026, 11, 3, 16, 20, 0));
        result.Note.Should().Be("Coffee and cake");
    }

    [Fact]
    public async Task GetById_NonExistentTransaction_ReturnsNull()
    {
        // arrange
        var repository = CreateRepository();
        var nonExistentId = Guid.NewGuid();

        // act
        var result = await repository.GetById(nonExistentId, CancellationToken.None);

        // assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Update_ExistingTransaction_UpdatesDatabaseAndCreatesOutboxMessage()
    {
        // arrange
        var repository = CreateRepository();
        var tx = new Transaction(
            Guid.NewGuid(),
            TransactionType.Expense,
            foodCategory,
            100m,
            new DateTime(2026, 11, 1, 10, 0, 0),
            "Original note");

        await repository.AddRange([tx], CancellationToken.None);

        await using (var clearOutboxContext = new SqliteDbContext(options))
        {
            clearOutboxContext.OutboxMessages.RemoveRange(clearOutboxContext.OutboxMessages);
            await clearOutboxContext.SaveChangesAsync();
        }

        var updatedTx = new Transaction(
            tx.Id,
            TransactionType.Income,
            salaryCategory,
            2500.50m,
            new DateTime(2026, 11, 5, 15, 30, 0),
            "Updated note");

        // act
        await repository.Update(updatedTx, CancellationToken.None);

        // assert
        await using var assertContext = new SqliteDbContext(options);
        var entity = await assertContext.Transactions.FirstOrDefaultAsync(t => t.Id == tx.Id);
        entity.Should().NotBeNull();
        entity!.Type.Should().Be(TransactionType.Income);
        entity.CategoryId.Should().Be("salary");
        entity.Amount.Should().Be(2500.50m);
        entity.Date.Should().Be(new DateTime(2026, 11, 5, 15, 30, 0));
        entity.Note.Should().Be("Updated note");

        var outboxMessages = await assertContext.OutboxMessages.ToListAsync();
        outboxMessages.Should().HaveCount(1);

        var outboxMessage = outboxMessages[0];
        outboxMessage.OperationType.Should().Be(OutboxOperationType.UpdateTransaction);
        outboxMessage.CreatedAtUtc.Should().Be(fixedTime);

        var payload = JsonSerializer.Deserialize<UpdateTransactionOutboxPayload>(outboxMessage.PayloadJson);
        payload.Should().NotBeNull();
        payload!.Transaction.Should().BeEquivalentTo(new TransactionOutboxDto(
            updatedTx.Id,
            TransactionType.Income,
            "salary",
            2500.50m,
            new DateTime(2026, 11, 5, 15, 30, 0),
            "Updated note"));
    }

    [Fact]
    public async Task Update_NonExistentTransaction_ThrowsSqlitePersistenceException()
    {
        // arrange
        var repository = CreateRepository();
        var nonExistentTx = new Transaction(
            Guid.NewGuid(),
            TransactionType.Expense,
            foodCategory,
            50m,
            new DateTime(2026, 11, 1, 10, 0, 0),
            null);

        // act
        var act = () => repository.Update(nonExistentTx, CancellationToken.None);

        // assert
        await act.Should().ThrowAsync<SqlitePersistenceException>()
            .WithMessage($"*{nonExistentTx.Id}*");
    }

    private sealed class TestDbContextFactory(DbContextOptions<SqliteDbContext> options) : IDbContextFactory<SqliteDbContext>
    {
        public SqliteDbContext CreateDbContext() => new(options);

        public Task<SqliteDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new SqliteDbContext(options));
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
