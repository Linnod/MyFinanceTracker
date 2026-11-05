namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Models;

internal sealed record AddTransactionsOutboxPayload(IReadOnlyList<TransactionOutboxDto> Transactions);
