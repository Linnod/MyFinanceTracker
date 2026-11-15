namespace MyFinanceTracker.Infrastructure.Persistence.Models;

internal sealed record CategorySyncRecord(
    string Id,
    string Name,
    bool IsArchived);
