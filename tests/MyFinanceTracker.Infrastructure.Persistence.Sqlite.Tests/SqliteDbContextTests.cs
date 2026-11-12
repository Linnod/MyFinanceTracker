using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Data;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Entities;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Tests;

public class SqliteDbContextTests : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly DbContextOptions<SqliteDbContext> options;

    public SqliteDbContextTests()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        options = new DbContextOptionsBuilder<SqliteDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new SqliteDbContext(options);
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task CanInsertAndRetrieve_CategoryEntity()
    {
        // arrange
        await using var context = new SqliteDbContext(options);
        var category = new CategoryEntity
        {
            Id = "groceries",
            Name = "Groceries",
            IsArchived = false
        };

        // act
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        // assert
        await using var assertContext = new SqliteDbContext(options);
        var retrieved = await assertContext.Categories.FindAsync(category.Id);

        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be("groceries");
        retrieved.Name.Should().Be("Groceries");
        retrieved.IsArchived.Should().BeFalse();
    }

    [Fact]
    public async Task CanInsertAndRetrieve_TransactionEntity()
    {
        // arrange
        await using var context = new SqliteDbContext(options);
        var category = new CategoryEntity
        {
            Id = "groceries",
            Name = "Groceries",
            IsArchived = false
        };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var entity = new TransactionEntity
        {
            Id = Guid.NewGuid(),
            Type = TransactionType.Expense,
            CategoryId = "groceries",
            Amount = 42.50m,
            Date = new DateTime(2026, 11, 5, 12, 30, 0),
            Note = "Supermarket"
        };

        // act
        context.Transactions.Add(entity);
        await context.SaveChangesAsync();

        // assert
        await using var assertContext = new SqliteDbContext(options);
        var retrieved = await assertContext.Transactions.FindAsync(entity.Id);

        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(entity.Id);
        retrieved.Type.Should().Be(TransactionType.Expense);
        retrieved.CategoryId.Should().Be("groceries");
        retrieved.Amount.Should().Be(42.50m);
        retrieved.Date.Should().Be(new DateTime(2026, 11, 5, 12, 30, 0));
        retrieved.Note.Should().Be("Supermarket");
    }

    [Fact]
    public async Task CanInsertAndRetrieve_OutboxMessageEntity_WithAutoIncrementId()
    {
        // arrange
        await using var context = new SqliteDbContext(options);
        var message1 = new OutboxMessageEntity
        {
            OperationType = OutboxOperationType.AddTransaction,
            PayloadJson = "{\"id\":\"1\"}",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        var message2 = new OutboxMessageEntity
        {
            OperationType = OutboxOperationType.DeleteTransaction,
            PayloadJson = "{\"id\":\"2\"}",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        // act
        context.OutboxMessages.AddRange(message1, message2);
        await context.SaveChangesAsync();

        // assert
        message1.Id.Should().BeGreaterThan(0);
        message2.Id.Should().BeGreaterThan(message1.Id);

        await using var assertContext = new SqliteDbContext(options);
        var messages = await assertContext.OutboxMessages.OrderBy(m => m.Id).ToListAsync();

        messages.Should().HaveCount(2);
        messages[0].OperationType.Should().Be(OutboxOperationType.AddTransaction);
        messages[0].PayloadJson.Should().Be("{\"id\":\"1\"}");
        messages[1].OperationType.Should().Be(OutboxOperationType.DeleteTransaction);
        messages[1].PayloadJson.Should().Be("{\"id\":\"2\"}");
    }

    [Fact]
    public async Task CanInsertAndRetrieve_DeadLetterMessageEntity()
    {
        // arrange
        await using var context = new SqliteDbContext(options);
        var deadLetter = new DeadLetterMessageEntity
        {
            OriginalOutboxMessageId = 42,
            OperationType = OutboxOperationType.AddTransaction,
            PayloadJson = "{\"payload\":\"test\"}",
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
            FailedAtUtc = DateTimeOffset.UtcNow,
            RetryCount = 5,
            ErrorMessage = "Network connection failed"
        };

        // act
        context.DeadLetterMessages.Add(deadLetter);
        await context.SaveChangesAsync();

        // assert
        deadLetter.Id.Should().BeGreaterThan(0);

        await using var assertContext = new SqliteDbContext(options);
        var retrieved = await assertContext.DeadLetterMessages.FindAsync(deadLetter.Id);

        retrieved.Should().NotBeNull();
        retrieved!.OriginalOutboxMessageId.Should().Be(42);
        retrieved.OperationType.Should().Be(OutboxOperationType.AddTransaction);
        retrieved.RetryCount.Should().Be(5);
        retrieved.ErrorMessage.Should().Be("Network connection failed");
    }

    public void Dispose()
    {
        connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
