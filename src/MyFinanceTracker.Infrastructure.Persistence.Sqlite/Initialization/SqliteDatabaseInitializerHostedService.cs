using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Common;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Data;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Entities;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Sync;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Initialization;

internal sealed partial class SqliteDatabaseInitializerHostedService(
    IDbContextFactory<SqliteDbContext> dbContextFactory,
    [FromKeyedServices(PersistenceServiceKeys.RemoteTransactionRepository)] ITransactionRepository transactionRepository,
    [FromKeyedServices(PersistenceServiceKeys.RemoteCategoryRepository)] ICategoryRepository remoteCategoryRepository,
    OutboxProcessor outboxProcessor,
    ILogger<SqliteDatabaseInitializerHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        LogInitializingDatabase();
        await EnsureDatabaseCreated(ct);

        LogFlushingOutbox();
        await outboxProcessor.ProcessPendingMessages(ct);

        await SyncCategories(ct);
        await SyncTransactions(ct);
    }

    public async Task StopAsync(CancellationToken ct)
    {
        LogStoppingInitialization();
        try
        {
            await outboxProcessor.ProcessPendingMessages(ct);
            LogStopCompleted();
        }
        catch (Exception ex)
        {
            LogFinalOutboxProcessingFailed(ex);
        }
    }

    private async Task EnsureDatabaseCreated(CancellationToken ct)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        await dbContext.Database.EnsureCreatedAsync(ct);
        await dbContext.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", ct);
    }

    private async Task SyncCategories(CancellationToken ct)
    {
        LogFetchingCategories();
        var remoteCategories = await remoteCategoryRepository.GetAll(ct);

        LogSyncingCategories(remoteCategories.Count);
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var existingTransactions = await dbContext.Transactions.ToListAsync(ct);
        dbContext.Transactions.RemoveRange(existingTransactions);

        var existingCategories = await dbContext.Categories.ToListAsync(ct);
        dbContext.Categories.RemoveRange(existingCategories);

        if (remoteCategories.Count > 0)
        {
            var entities = remoteCategories.Select(c => new CategoryEntity
            {
                Id = c.Id,
                Name = c.Name,
                IsArchived = c.IsArchived
            });

            await dbContext.Categories.AddRangeAsync(entities, ct);
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private async Task SyncTransactions(CancellationToken ct)
    {
        LogFetchingTransactions();
        var remoteTransactions = await transactionRepository.GetAll(ct);

        LogSyncingTransactions(remoteTransactions.Count);
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        if (remoteTransactions.Count > 0)
        {
            var entities = remoteTransactions.Select(t => new TransactionEntity
            {
                Id = t.Id,
                Type = t.Type,
                CategoryId = t.Category.Id,
                Amount = t.Amount,
                Date = t.Date,
                Note = t.Note
            });

            await dbContext.Transactions.AddRangeAsync(entities, ct);
        }

        await dbContext.SaveChangesAsync(ct);

        LogDatabaseInitialized(remoteTransactions.Count);
    }
}
