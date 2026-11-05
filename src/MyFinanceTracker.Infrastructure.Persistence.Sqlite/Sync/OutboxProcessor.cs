using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyFinanceTracker.Common;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Configuration;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Data;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Entities;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Models;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Sync.Exceptions;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Sync;

internal sealed partial class OutboxProcessor(
    IDbContextFactory<SqliteDbContext> dbContextFactory,
    [FromKeyedServices(PersistenceServiceKeys.RemoteTransactionRepository)] ITransactionRepository transactionRepository,
    ICategoryRepository categoryRepository,
    TimeProvider timeProvider,
    IOptions<SqlitePersistenceOptions> options,
    ILogger<OutboxProcessor> logger) : IDisposable
{
    private readonly SemaphoreSlim semaphore = new(1, 1);

    public async Task ProcessPendingMessages(CancellationToken ct)
    {
        await semaphore.WaitAsync(ct);
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
            var messages = await dbContext.OutboxMessages
                .OrderBy(m => m.Id)
                .ToListAsync(ct);

            if (messages.Count == 0)
            {
                return;
            }

            LogProcessingPendingMessages(messages.Count);

            foreach (var message in messages)
            {
                var shouldContinue = await ProcessMessage(dbContext, message, ct);
                if (!shouldContinue)
                {
                    break;
                }
            }

            LogMessagesCompleted();
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<bool> ProcessMessage(
        SqliteDbContext dbContext,
        OutboxMessageEntity message,
        CancellationToken ct)
    {
        try
        {
            var task = message.OperationType switch
            {
                OutboxOperationType.Add => ProcessAdd(message, ct),
                OutboxOperationType.Delete => ProcessDelete(message, ct),
                _ => throw OutboxProcessingException.UnsupportedOperation(message)
            };

            await task;

            dbContext.OutboxMessages.Remove(message);
            await dbContext.SaveChangesAsync(ct);

            LogMessageProcessed(message);
            return true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            message.RetryCount++;

            if (message.RetryCount >= options.Value.MaxRetryAttempts)
            {
                await MoveToDeadLetter(dbContext, message, ex, ct);
                return true;
            }

            await dbContext.SaveChangesAsync(ct);
            LogRetryScheduled(ex, message, message.RetryCount, options.Value.MaxRetryAttempts);
            return false;
        }
    }

    private async Task MoveToDeadLetter(
        SqliteDbContext dbContext,
        OutboxMessageEntity message,
        Exception ex,
        CancellationToken ct)
    {
        var deadLetter = new DeadLetterMessageEntity
        {
            OriginalOutboxMessageId = message.Id,
            OperationType = message.OperationType,
            PayloadJson = message.PayloadJson,
            CreatedAtUtc = message.CreatedAtUtc,
            FailedAtUtc = timeProvider.GetUtcNow(),
            RetryCount = message.RetryCount,
            ErrorMessage = ex.ToString()
        };

        dbContext.OutboxMessages.Remove(message);
        dbContext.DeadLetterMessages.Add(deadLetter);
        await dbContext.SaveChangesAsync(ct);

        LogMovedToDeadLetter(ex, message);
    }

    private async Task ProcessAdd(OutboxMessageEntity message, CancellationToken ct)
    {
        var payload = DeserializePayload<AddTransactionsOutboxPayload>(message);

        var transactions = new List<Transaction>(payload.Transactions.Count);
        foreach (var dto in payload.Transactions)
        {
            var category = await categoryRepository.Resolve(dto.CategoryId, ct)
                ?? throw OutboxProcessingException.CategoryNotFound(message, dto);

            transactions.Add(new Transaction(
                dto.Id,
                dto.Type,
                category,
                dto.Amount,
                dto.Date,
                dto.Note));
        }

        await transactionRepository.AddRange(transactions, ct);
    }

    private async Task ProcessDelete(OutboxMessageEntity message, CancellationToken ct)
    {
        var payload = DeserializePayload<DeleteTransactionsOutboxPayload>(message);

        await transactionRepository.DeleteRange(payload.Ids, ct);
    }

    private static T DeserializePayload<T>(OutboxMessageEntity message)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(message.PayloadJson)
                ?? throw OutboxProcessingException.DeserializationFailed(message);
        }
        catch (JsonException ex)
        {
            throw OutboxProcessingException.DeserializationFailed(message, ex);
        }
    }

    public void Dispose()
    {
        semaphore.Dispose();
    }
}
