using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Data;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Entities;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Exceptions;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Models;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Repositories;

internal sealed partial class SqliteCategoryRepository(
    IDbContextFactory<SqliteDbContext> dbContextFactory,
    TimeProvider timeProvider,
    ILogger<SqliteCategoryRepository> logger) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> GetAll(CancellationToken ct)
    {
        LogGettingAllCategories();

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var entities = await dbContext.Categories
            .AsNoTracking()
            .ToListAsync(ct);

        return [.. entities.Select(ToDomain)];
    }

    public async Task<Category?> GetById(string id, CancellationToken ct)
    {
        LogGettingCategoryById(id);

        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var trimmedId = id.Trim();
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var entity = await dbContext.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == trimmedId, ct);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IReadOnlyList<Category>> GetByIds(IReadOnlyCollection<string> ids, CancellationToken ct)
    {
        LogGettingCategoriesByIds(ids.Count);

        if (ids.Count == 0)
        {
            return [];
        }

        var normalizedIds = ids
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .ToList();

        if (normalizedIds.Count == 0)
        {
            return [];
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var entities = await dbContext.Categories
            .AsNoTracking()
            .Where(c => normalizedIds.Contains(c.Id))
            .ToListAsync(ct);

        return [.. entities.Select(ToDomain)];
    }

    public async Task Add(Category category, CancellationToken ct)
    {
        LogAddingCategory(category.Id);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var entity = new CategoryEntity
        {
            Id = category.Id,
            Name = category.Name,
            IsArchived = category.IsArchived
        };

        dbContext.Categories.Add(entity);

        var payload = new AddCategoryOutboxPayload(ToOutboxDto(category));
        var outboxMessage = new OutboxMessageEntity
        {
            OperationType = OutboxOperationType.AddCategory,
            PayloadJson = JsonSerializer.Serialize(payload),
            CreatedAtUtc = timeProvider.GetUtcNow()
        };

        dbContext.OutboxMessages.Add(outboxMessage);

        await dbContext.SaveChangesAsync(ct);
    }

    public async Task Update(Category category, CancellationToken ct)
    {
        LogUpdatingCategory(category.Id);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var entity = await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == category.Id, ct);
        if (entity is null)
        {
            throw SqlitePersistenceException.CategoryNotFound(category.Id);
        }

        entity.Name = category.Name;
        entity.IsArchived = category.IsArchived;

        var payload = new UpdateCategoryOutboxPayload(ToOutboxDto(category));
        var outboxMessage = new OutboxMessageEntity
        {
            OperationType = OutboxOperationType.UpdateCategory,
            PayloadJson = JsonSerializer.Serialize(payload),
            CreatedAtUtc = timeProvider.GetUtcNow()
        };

        dbContext.OutboxMessages.Add(outboxMessage);

        await dbContext.SaveChangesAsync(ct);
    }

    private static Category ToDomain(CategoryEntity entity) => new(
        entity.Id,
        entity.Name,
        entity.IsArchived);

    private static CategoryOutboxDto ToOutboxDto(Category category) => new(
        category.Id,
        category.Name,
        category.IsArchived);
}
