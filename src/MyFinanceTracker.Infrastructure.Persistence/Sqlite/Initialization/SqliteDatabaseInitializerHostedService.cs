using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Infrastructure.Persistence.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Data;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Entities;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Exceptions;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Sync;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Initialization;

internal sealed partial class SqliteDatabaseInitializerHostedService(
    IDbContextFactory<SqliteDbContext> dbContextFactory,
    IRemoteTransactionRepository transactionRepository,
    IRemoteCategoryRepository remoteCategoryRepository,
    OutboxProcessor outboxProcessor,
    ILogger<SqliteDatabaseInitializerHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        LogInitializingDatabase();
        await EnsureDatabaseCreated(ct);

        LogFlushingOutbox();
        await outboxProcessor.ProcessPendingMessages(ct);

        if (await ShouldSkipInitialSync(ct))
        {
            return;
        }

        await SyncFromRemote(ct);
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

    private async Task<bool> ShouldSkipInitialSync(CancellationToken ct)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var hasData = await dbContext.Categories.AnyAsync(ct)
            || await dbContext.Transactions.AnyAsync(ct)
            || await dbContext.OutboxMessages.AnyAsync(ct)
            || await dbContext.DeadLetterMessages.AnyAsync(ct);

        if (!hasData)
        {
            return false;
        }

        LogSkippingRemoteSync();
        return true;
    }

    private async Task SyncFromRemote(CancellationToken ct)
    {
        LogFetchingCategories();
        var remoteCategories = await remoteCategoryRepository.GetAll(ct);

        LogFetchingTransactions();
        var remoteTransactions = await transactionRepository.GetAll(ct);

        if (remoteCategories.Count == 0 && remoteTransactions.Count == 0)
        {
            LogDatabaseInitialized(0);
            return;
        }

        if (remoteTransactions.Count > 0)
        {
            var knownCategoryIds = remoteCategories
                .Select(c => c.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var unknownTransaction = remoteTransactions.FirstOrDefault(t => !knownCategoryIds.Contains(t.CategoryId));
            if (unknownTransaction is not null)
            {
                throw SqlitePersistenceException.CategoryNotFound(unknownTransaction.CategoryId);
            }
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        if (remoteCategories.Count > 0)
        {
            LogSyncingCategories(remoteCategories.Count);
            var categoryEntities = remoteCategories
                .Select(c => new CategoryEntity
                {
                    Id = c.Id,
                    Name = c.Name,
                    IsArchived = c.IsArchived
                });

            await dbContext.Categories.AddRangeAsync(categoryEntities, ct);
        }

        if (remoteTransactions.Count > 0)
        {
            LogSyncingTransactions(remoteTransactions.Count);
            var transactionEntities = remoteTransactions
                .Select(t => new TransactionEntity
                {
                    Id = t.Id,
                    Type = t.Type,
                    CategoryId = t.CategoryId,
                    Amount = t.Amount,
                    Date = t.Date,
                    Note = t.Note
                });

            await dbContext.Transactions.AddRangeAsync(transactionEntities, ct);
        }

        await dbContext.SaveChangesAsync(ct);

        LogDatabaseInitialized(remoteTransactions.Count);
    }
}
