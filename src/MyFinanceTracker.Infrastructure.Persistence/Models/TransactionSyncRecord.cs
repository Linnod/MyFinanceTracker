using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.Infrastructure.Persistence.Models;

internal sealed record TransactionSyncRecord(
    Guid Id,
    TransactionType Type,
    string CategoryId,
    decimal Amount,
    DateTime Date,
    string? Note);
