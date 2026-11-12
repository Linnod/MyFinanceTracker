using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Data;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Entities;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Exceptions;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Models;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Repositories;
using NSubstitute;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Tests.Repositories;

public sealed class SqliteCategoryRepositoryTests : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly DbContextOptions<SqliteDbContext> options;
    private readonly IDbContextFactory<SqliteDbContext> contextFactory;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<SqliteCategoryRepository> logger;
    private readonly DateTimeOffset fixedTime = new(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);

    public SqliteCategoryRepositoryTests()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        options = new DbContextOptionsBuilder<SqliteDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new SqliteDbContext(options);
        context.Database.EnsureCreated();

        contextFactory = new TestDbContextFactory(options);
        timeProvider = new FixedTimeProvider(fixedTime);
        logger = Substitute.For<ILogger<SqliteCategoryRepository>>();
    }

    public void Dispose()
    {
        connection.Dispose();
    }

    private SqliteCategoryRepository CreateRepository() =>
        new(contextFactory, timeProvider, logger);

    [Fact]
    public async Task GetAll_EmptyDatabase_ReturnsEmptyList()
    {
        // arrange
        var sut = CreateRepository();

        // act
        var result = await sut.GetAll(CancellationToken.None);

        // assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_ExistingCategories_ReturnsAllCategories()
    {
        // arrange
        var sut = CreateRepository();
        await using (var seedContext = new SqliteDbContext(options))
        {
            seedContext.Categories.AddRange(
                new CategoryEntity { Id = "food", Name = "Food", IsArchived = false },
                new CategoryEntity { Id = "salary", Name = "Salary", IsArchived = true });
            await seedContext.SaveChangesAsync();
        }

        // act
        var result = await sut.GetAll(CancellationToken.None);

        // assert
        result.Should().HaveCount(2);
        result.Should().ContainEquivalentOf(new Category("food", "Food", false));
        result.Should().ContainEquivalentOf(new Category("salary", "Salary", true));
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsCategory()
    {
        // arrange
        var sut = CreateRepository();
        await using (var seedContext = new SqliteDbContext(options))
        {
            seedContext.Categories.Add(new CategoryEntity { Id = "food", Name = "Food", IsArchived = false });
            await seedContext.SaveChangesAsync();
        }

        // act
        var result = await sut.GetById("food", CancellationToken.None);

        // assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("food");
        result.Name.Should().Be("Food");
        result.IsArchived.Should().BeFalse();
    }

    [Theory]
    [InlineData("FOOD")]
    [InlineData("Food")]
    [InlineData(" food ")]
    public async Task GetById_CaseInsensitiveMatch_ReturnsCategory(string searchId)
    {
        // arrange
        var sut = CreateRepository();
        await using (var seedContext = new SqliteDbContext(options))
        {
            seedContext.Categories.Add(new CategoryEntity { Id = "food", Name = "Food", IsArchived = false });
            await seedContext.SaveChangesAsync();
        }

        // act
        var result = await sut.GetById(searchId, CancellationToken.None);

        // assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("food");
        result.Name.Should().Be("Food");
    }

    [Fact]
    public async Task GetById_NonExistentId_ReturnsNull()
    {
        // arrange
        var sut = CreateRepository();

        // act
        var result = await sut.GetById("unknown", CancellationToken.None);

        // assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetById_NullOrWhitespace_ReturnsNull(string? id)
    {
        // arrange
        var sut = CreateRepository();

        // act
        var result = await sut.GetById(id!, CancellationToken.None);

        // assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIds_EmptyList_ReturnsEmptyList()
    {
        // arrange
        var sut = CreateRepository();

        // act
        var result = await sut.GetByIds([], CancellationToken.None);

        // assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIds_ExistingIds_ReturnsMatchingCategories()
    {
        // arrange
        var sut = CreateRepository();
        await using (var seedContext = new SqliteDbContext(options))
        {
            seedContext.Categories.AddRange(
                new CategoryEntity { Id = "food", Name = "Food", IsArchived = false },
                new CategoryEntity { Id = "transport", Name = "Transport", IsArchived = false },
                new CategoryEntity { Id = "salary", Name = "Salary", IsArchived = true });
            await seedContext.SaveChangesAsync();
        }

        // act
        var result = await sut.GetByIds(["food", "salary"], CancellationToken.None);

        // assert
        result.Should().HaveCount(2);
        result.Should().Contain(c => c.Id == "food");
        result.Should().Contain(c => c.Id == "salary");
    }

    [Fact]
    public async Task GetByIds_CaseInsensitive_ReturnsMatchingCategories()
    {
        // arrange
        var sut = CreateRepository();
        await using (var seedContext = new SqliteDbContext(options))
        {
            seedContext.Categories.AddRange(
                new CategoryEntity { Id = "food", Name = "Food", IsArchived = false },
                new CategoryEntity { Id = "transport", Name = "Transport", IsArchived = false });
            await seedContext.SaveChangesAsync();
        }

        // act
        var result = await sut.GetByIds(["FOOD", " Transport "], CancellationToken.None);

        // assert
        result.Should().HaveCount(2);
        result.Should().Contain(c => c.Id == "food");
        result.Should().Contain(c => c.Id == "transport");
    }

    [Fact]
    public async Task GetByIds_NonExistentIds_ReturnsEmptyList()
    {
        // arrange
        var sut = CreateRepository();

        // act
        var result = await sut.GetByIds(["missing1", "missing2"], CancellationToken.None);

        // assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Add_ValidCategory_SavesToDatabaseAndCreatesOutboxMessage()
    {
        // arrange
        var sut = CreateRepository();
        var category = new Category("groceries", "Groceries & Daily", false);

        // act
        await sut.Add(category, CancellationToken.None);

        // assert
        await using var assertContext = new SqliteDbContext(options);
        var entity = await assertContext.Categories.FindAsync("groceries");
        entity.Should().NotBeNull();
        entity!.Id.Should().Be("groceries");
        entity.Name.Should().Be("Groceries & Daily");
        entity.IsArchived.Should().BeFalse();

        var outboxMessages = await assertContext.OutboxMessages.ToListAsync();
        outboxMessages.Should().HaveCount(1);

        var outboxMessage = outboxMessages[0];
        outboxMessage.OperationType.Should().Be(OutboxOperationType.AddCategory);
        outboxMessage.CreatedAtUtc.Should().Be(fixedTime);

        var payload = JsonSerializer.Deserialize<AddCategoryOutboxPayload>(outboxMessage.PayloadJson);
        payload.Should().NotBeNull();
        payload!.Category.Should().BeEquivalentTo(new CategoryOutboxDto("groceries", "Groceries & Daily", false));
    }

    [Fact]
    public async Task Update_ExistingCategory_UpdatesDatabaseAndCreatesOutboxMessage()
    {
        // arrange
        var sut = CreateRepository();
        await using (var seedContext = new SqliteDbContext(options))
        {
            seedContext.Categories.Add(new CategoryEntity
            {
                Id = "groceries",
                Name = "Groceries",
                IsArchived = false
            });
            await seedContext.SaveChangesAsync();
        }

        var updatedCategory = new Category("groceries", "Groceries & Supermarkets", true);

        // act
        await sut.Update(updatedCategory, CancellationToken.None);

        // assert
        await using var assertContext = new SqliteDbContext(options);
        var entity = await assertContext.Categories.FindAsync("groceries");
        entity.Should().NotBeNull();
        entity!.Name.Should().Be("Groceries & Supermarkets");
        entity.IsArchived.Should().BeTrue();

        var outboxMessages = await assertContext.OutboxMessages.ToListAsync();
        outboxMessages.Should().HaveCount(1);

        var outboxMessage = outboxMessages[0];
        outboxMessage.OperationType.Should().Be(OutboxOperationType.UpdateCategory);
        outboxMessage.CreatedAtUtc.Should().Be(fixedTime);

        var payload = JsonSerializer.Deserialize<UpdateCategoryOutboxPayload>(outboxMessage.PayloadJson);
        payload.Should().NotBeNull();
        payload!.Category.Should().BeEquivalentTo(new CategoryOutboxDto("groceries", "Groceries & Supermarkets", true));
    }

    [Fact]
    public async Task Update_NonExistentCategory_ThrowsSqlitePersistenceException()
    {
        // arrange
        var sut = CreateRepository();
        var category = new Category("non_existent", "Non Existent", false);

        // act
        var act = () => sut.Update(category, CancellationToken.None);

        // assert
        await act.Should().ThrowAsync<SqlitePersistenceException>()
            .WithMessage("*non_existent*");
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
