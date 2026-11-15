using MyFinanceTracker.Infrastructure.Persistence.Models;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Models;

internal sealed record AddTransactionsOutboxPayload(IReadOnlyList<TransactionSyncRecord> Transactions);
