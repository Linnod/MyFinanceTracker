namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Models;

internal sealed record DeleteTransactionsOutboxPayload(IReadOnlyList<Guid> Ids);
