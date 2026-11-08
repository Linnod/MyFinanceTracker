using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Models;

internal sealed record TransactionOutboxDto(
    Guid Id,
    TransactionType Type,
    string CategoryId,
    decimal Amount,
    DateTime Date,
    string? Note);
