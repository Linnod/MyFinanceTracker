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
    public async Task CanInsertAndRetrieve_TransactionEntity()
    {
        // Arrange
        await using var context = new SqliteDbContext(options);
        var entity = new TransactionEntity
        {
            Id = Guid.NewGuid(),
            Type = TransactionType.Expense,
            CategoryId = "groceries",
            Amount = 42.50m,
            Date = new DateOnly(2026, 11, 5),
            Note = "Supermarket"
        };

        // Act
        context.Transactions.Add(entity);
        await context.SaveChangesAsync();

        // Assert
        await using var assertContext = new SqliteDbContext(options);
        var retrieved = await assertContext.Transactions.FindAsync(entity.Id);

        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(entity.Id);
        retrieved.Type.Should().Be(TransactionType.Expense);
        retrieved.CategoryId.Should().Be("groceries");
        retrieved.Amount.Should().Be(42.50m);
        retrieved.Date.Should().Be(new DateOnly(2026, 11, 5));
        retrieved.Note.Should().Be("Supermarket");
    }

    [Fact]
    public async Task CanInsertAndRetrieve_OutboxMessageEntity_WithAutoIncrementId()
    {
        // Arrange
        await using var context = new SqliteDbContext(options);
        var message1 = new OutboxMessageEntity
        {
            OperationType = OutboxOperationType.Add,
            PayloadJson = "{\"id\":\"1\"}",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        var message2 = new OutboxMessageEntity
        {
            OperationType = OutboxOperationType.Delete,
            PayloadJson = "{\"id\":\"2\"}",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        // Act
        context.OutboxMessages.AddRange(message1, message2);
        await context.SaveChangesAsync();

        // Assert
        message1.Id.Should().BeGreaterThan(0);
        message2.Id.Should().BeGreaterThan(message1.Id);

        await using var assertContext = new SqliteDbContext(options);
        var messages = await assertContext.OutboxMessages.OrderBy(m => m.Id).ToListAsync();

        messages.Should().HaveCount(2);
        messages[0].OperationType.Should().Be(OutboxOperationType.Add);
        messages[0].PayloadJson.Should().Be("{\"id\":\"1\"}");
        messages[1].OperationType.Should().Be(OutboxOperationType.Delete);
        messages[1].PayloadJson.Should().Be("{\"id\":\"2\"}");
    }

    [Fact]
    public async Task CanInsertAndRetrieve_DeadLetterMessageEntity()
    {
        // Arrange
        await using var context = new SqliteDbContext(options);
        var deadLetter = new DeadLetterMessageEntity
        {
            OriginalOutboxMessageId = 42,
            OperationType = OutboxOperationType.Add,
            PayloadJson = "{\"payload\":\"test\"}",
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
            FailedAtUtc = DateTimeOffset.UtcNow,
            RetryCount = 5,
            ErrorMessage = "Network connection failed"
        };

        // Act
        context.DeadLetterMessages.Add(deadLetter);
        await context.SaveChangesAsync();

        // Assert
        deadLetter.Id.Should().BeGreaterThan(0);

        await using var assertContext = new SqliteDbContext(options);
        var retrieved = await assertContext.DeadLetterMessages.FindAsync(deadLetter.Id);

        retrieved.Should().NotBeNull();
        retrieved!.OriginalOutboxMessageId.Should().Be(42);
        retrieved.OperationType.Should().Be(OutboxOperationType.Add);
        retrieved.RetryCount.Should().Be(5);
        retrieved.ErrorMessage.Should().Be("Network connection failed");
    }

    public void Dispose()
    {
        connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
