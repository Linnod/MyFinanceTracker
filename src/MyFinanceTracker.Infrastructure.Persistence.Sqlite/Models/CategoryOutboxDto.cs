namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Models;

internal sealed record CategoryOutboxDto(
    string Id,
    string Name,
    bool IsArchived);
