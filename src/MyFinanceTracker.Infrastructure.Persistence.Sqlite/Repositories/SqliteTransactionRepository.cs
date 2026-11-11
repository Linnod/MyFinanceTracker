using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Domain.Common;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Data;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Entities;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Exceptions;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Models;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Repositories;

internal sealed partial class SqliteTransactionRepository(
    IDbContextFactory<SqliteDbContext> dbContextFactory,
    ICategoryRepository categoryRepository,
    TimeProvider timeProvider,
    ILogger<SqliteTransactionRepository> logger) : ITransactionRepository
{
    public async Task AddRange(IReadOnlyCollection<Transaction> transactions, CancellationToken ct)
    {
        if (transactions.Count == 0)
        {
            return;
        }

        LogAddingTransactions(transactions.Count);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var entities = transactions.Select(ToEntity).ToList();

        await dbContext.Transactions.AddRangeAsync(entities, ct);

        var outboxDtos = transactions.Select(ToOutboxDto).ToList();

        var payload = new AddTransactionsOutboxPayload(outboxDtos);
        var outboxMessage = new OutboxMessageEntity
        {
            OperationType = OutboxOperationType.Add,
            PayloadJson = JsonSerializer.Serialize(payload),
            CreatedAtUtc = timeProvider.GetUtcNow()
        };

        dbContext.OutboxMessages.Add(outboxMessage);

        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<int> DeleteRange(IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        LogDeletingTransactions(ids.Count);

        if (ids.Count == 0)
        {
            return 0;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var idsSet = ids.Distinct().ToList();

        var existingEntities = await dbContext.Transactions
            .Where(t => idsSet.Contains(t.Id))
            .ToListAsync(ct);

        if (existingEntities.Count == 0)
        {
            return 0;
        }

        dbContext.Transactions.RemoveRange(existingEntities);

        var payload = new DeleteTransactionsOutboxPayload(existingEntities.Select(t => t.Id).ToList());
        var outboxMessage = new OutboxMessageEntity
        {
            OperationType = OutboxOperationType.Delete,
            PayloadJson = JsonSerializer.Serialize(payload),
            CreatedAtUtc = timeProvider.GetUtcNow()
        };

        dbContext.OutboxMessages.Add(outboxMessage);

        await dbContext.SaveChangesAsync(ct);

        return existingEntities.Count;
    }

    public async Task<Transaction?> GetById(Guid id, CancellationToken ct)
    {
        LogGettingTransactionById(id);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var entity = await dbContext.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        return entity is null ? null : await ToDomain(entity, ct);
    }

    public async Task Update(Transaction transaction, CancellationToken ct)
    {
        LogUpdatingTransaction(transaction.Id);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var entity = await dbContext.Transactions.FirstOrDefaultAsync(t => t.Id == transaction.Id, ct);
        if (entity is null)
        {
            throw SqlitePersistenceException.TransactionNotFound(transaction.Id);
        }

        UpdateEntity(entity, transaction);

        var payload = new UpdateTransactionOutboxPayload(ToOutboxDto(transaction));
        var outboxMessage = new OutboxMessageEntity
        {
            OperationType = OutboxOperationType.Update,
            PayloadJson = JsonSerializer.Serialize(payload),
            CreatedAtUtc = timeProvider.GetUtcNow()
        };

        dbContext.OutboxMessages.Add(outboxMessage);

        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Transaction>> GetAll(CancellationToken ct)
    {
        LogGettingAllTransactions();

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var entities = await dbContext.Transactions
            .AsNoTracking()
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .ToListAsync(ct);

        return await ToDomain(entities, ct);
    }

    public async Task<PagedResult<Transaction>> Get(TransactionFilter filter, CancellationToken ct)
    {
        LogGettingTransactions(filter);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var query = dbContext.Transactions.AsNoTracking();

        if (filter.FromDate.HasValue)
        {
            query = query.Where(t => t.Date >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(t => t.Date <= filter.ToDate.Value);
        }

        if (filter.Type.HasValue)
        {
            query = query.Where(t => t.Type == filter.Type.Value);
        }

        if (filter.Categories is { Count: > 0 })
        {
            var categoryIds = filter.Categories.Select(c => c.Id).ToList();
            query = query.Where(t => categoryIds.Contains(t.CategoryId));
        }

        var totalCount = await query.CountAsync(ct);
        if (totalCount == 0 || filter.Take == 0)
        {
            return new PagedResult<Transaction>([], totalCount);
        }

        query = query.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id);

        if (filter.Skip.HasValue)
        {
            query = query.Skip(filter.Skip.Value);
        }

        if (filter.Take.HasValue)
        {
            query = query.Take(filter.Take.Value);
        }

        var entities = await query.ToListAsync(ct);
        var transactions = await ToDomain(entities, ct);

        return new PagedResult<Transaction>(transactions, totalCount);
    }

    private async Task<Transaction> ToDomain(TransactionEntity entity, CancellationToken ct)
    {
        var category = await categoryRepository.GetById(entity.CategoryId, ct)
            ?? throw SqlitePersistenceException.CategoryNotFound(entity.Id, entity.CategoryId);

        return new Transaction(
            entity.Id,
            entity.Type,
            category,
            entity.Amount,
            entity.Date,
            entity.Note);
    }

    private async Task<List<Transaction>> ToDomain(List<TransactionEntity> entities, CancellationToken ct)
    {
        var transactions = new List<Transaction>(entities.Count);
        foreach (var entity in entities)
        {
            transactions.Add(await ToDomain(entity, ct));
        }

        return transactions;
    }

    private static TransactionEntity ToEntity(Transaction transaction) => new()
    {
        Id = transaction.Id,
        Type = transaction.Type,
        CategoryId = transaction.Category.Id,
        Amount = transaction.Amount,
        Date = transaction.Date,
        Note = transaction.Note
    };

    private static void UpdateEntity(TransactionEntity entity, Transaction transaction)
    {
        entity.Type = transaction.Type;
        entity.CategoryId = transaction.Category.Id;
        entity.Amount = transaction.Amount;
        entity.Date = transaction.Date;
        entity.Note = transaction.Note;
    }

    private static TransactionOutboxDto ToOutboxDto(Transaction transaction) => new(
        transaction.Id,
        transaction.Type,
        transaction.Category.Id,
        transaction.Amount,
        transaction.Date,
        transaction.Note);
}
